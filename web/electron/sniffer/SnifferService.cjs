const { spawn } = require('child_process');
const { getDofusConnection } = require('./SnifferDetector.cjs');
const logger = require('../logger/LoggerService.cjs');

const SVC = 'SnifferService';

class SnifferService {
    constructor() {
        this.process = null;
        this.dataBuffer = '';
        this.bankBuffer = '';
        this.parseTimeout = null;

        this.mainWindow = null;
        this.currentConfig = null;

        this.modules = {
            hdv: true,
            bank: true
        };
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    getActiveConfig() {
        return this.currentConfig;
    }

    updateModules(config) {
        this.modules = { ...this.modules, ...config };
        logger.info(SVC, 'Modules mis à jour', this.modules);

        if (!this.modules.hdv && !this.modules.bank && this.process) {
            logger.info(SVC, 'Tous les modules désactivés — arrêt du sniffer');
            this.stop();
        }
    }

    start(forcedConfig = null) {
        if (this.process) {
            logger.warn(SVC, 'Processus déjà actif — arrêt avant redémarrage');
            this.stop();
        }

        let config = forcedConfig;
        if (!config) {
            logger.info(SVC, 'Détection automatique de la connexion Dofus...');
            config = getDofusConnection();
        }

        if (!config) {
            logger.error(SVC, 'Dofus non détecté — impossible de démarrer');
            throw new Error("Dofus non détecté.");
        }

        this.currentConfig = config;
        logger.info(SVC, 'Démarrage moteur tcpdump', config);

        const args = ['-i', 'any', '-A', '-l', '-nn', '-q', 'tcp'];

        if (config.localPort) {
            args.push('port', config.localPort.toString());
            logger.debug(SVC, `Filtre : port local ${config.localPort}`);
        } else {
            args.push('port', config.remotePort.toString(), 'and', 'host', config.remoteIp);
            logger.debug(SVC, `Filtre : port ${config.remotePort} host ${config.remoteIp}`);
        }

        this.process = spawn('tcpdump', args);
        logger.info(SVC, `tcpdump démarré (PID ${this.process.pid})`);

        this.process.stdout.on('data', (data) => {
            const chunk = data.toString('latin1');

            if (this.modules.hdv) {
                this.dataBuffer += chunk;
                clearTimeout(this.parseTimeout);
                this.parseTimeout = setTimeout(() => {
                    this.processFullBuffer();
                }, 50);
            }

            if (this.modules.bank) {
                this.processBankPacket(chunk);
            }
        });

        this.process.stderr.on('data', (data) => {
            const msg = data.toString().trim();
            if (msg) logger.warn(SVC, `tcpdump stderr : ${msg}`);
        });

        this.process.on('error', (err) => {
            logger.error(SVC, `Erreur processus tcpdump : ${err.message}`, { code: err.code });
        });

        this.process.on('exit', (code, signal) => {
            logger.info(SVC, 'Moteur tcpdump arrêté', { code, signal });
            this.process = null;
        });
    }

    processBankPacket(chunk) {
        this.bankBuffer += chunk;

        if (this.bankBuffer.includes('EL') && this.mainWindow && !this.mainWindow.isDestroyed()) {
            logger.info(SVC, 'Dump banque complet détecté (EL)');
            this.mainWindow.webContents.send('bank-full-dump');
        }

        const regex = /O[0-9a-f]+~[0-9a-f]+~[0-9a-f]+~[^;|\0]*/g;
        let match;

        while ((match = regex.exec(this.bankBuffer)) !== null) {
            const rawItem = match[0];
            if (rawItem.includes('~')) {
                const parsed = this.parseBankItem(rawItem);
                logger.debug(SVC, `Item banque capturé : assetId=${parsed.assetId} qty=${parsed.quantity}`);
                if (this.mainWindow && !this.mainWindow.isDestroyed()) {
                    this.mainWindow.webContents.send('bank-item-captured', parsed);
                }
            }
        }

        const lastO = this.bankBuffer.lastIndexOf(';O');
        if (lastO !== -1) {
            this.bankBuffer = this.bankBuffer.substring(lastO + 1);
        } else if (this.bankBuffer.length > 5000) {
            this.bankBuffer = '';
        }
    }

    parseBankItem(raw) {
        const content = raw.startsWith('O') ? raw.substring(1) : raw;
        const parts = content.split('~');
        return {
            instanceId: parts[0],
            assetId: parseInt(parts[1], 16),
            quantity: parseInt(parts[2], 16) || 1,
            position: parts[3] || null,
            stats: parts[4] || ''
        };
    }

    processFullBuffer() {
        const ehlIndex = this.dataBuffer.indexOf('EHl');
        if (ehlIndex === -1) {
            this.dataBuffer = '';
            return;
        }
        const results = this.parseHDV(this.dataBuffer.substring(ehlIndex));
        if (results.length > 0) {
            logger.info(SVC, `HDV parsé : ${results.length} instance(s) pour itemId ${results[0]?.itemId}`);
            if (this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('sniffer:data', results);
            }
        }
        this.dataBuffer = '';
    }

    parseHDV(rawContent) {
        const segments = rawContent.split('|');
        if (segments.length < 2) return [];
        const itemIdMatch = segments[0].match(/EHl(\d+)/);
        if (!itemIdMatch) return [];
        const itemId = parseInt(itemIdMatch[1]);
        const allInstances = [];

        for (let i = 1; i < segments.length; i++) {
            const fields = segments[i].split(';');
            if (fields.length < 5) continue;
            const cleanValue = (val) => {
                if (!val) return 0;
                const beforeComma = val.split(',')[0];
                const digitsOnly = beforeComma.replace(/\D/g, '');
                const num = parseInt(digitsOnly);
                return isNaN(num) ? 0 : num;
            };
            allInstances.push({
                itemId: itemId,
                instanceId: fields[0].replace(/\D/g, ''),
                p1: cleanValue(fields[2]),
                p10: cleanValue(fields[3]),
                p100: cleanValue(fields[4])
            });
        }
        return allInstances;
    }

    stop() {
        if (this.process) {
            logger.info(SVC, 'Arrêt du sniffer (SIGINT)');
            this.process.kill('SIGINT');
            this.process = null;
        }
        this.dataBuffer = '';
        this.bankBuffer = '';
        logger.info(SVC, 'Sniffer stoppé, buffers vidés');
    }
}

module.exports = new SnifferService();
