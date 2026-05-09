const { spawn, execSync } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const { checkRequirements } = require('./SnifferDoctor.cjs');

const SVC = 'AutofocusService';

class AutofocusService {
    constructor() {
        this.process = null;
        this.mapping = {};
        this.initiativeOrder = [];
        this.lastCharId = null;
        this.mainWindow = null;
        this.switcherService = null;
        this.interface = 'any'; // Par défaut 'any' pour plus de compatibilité
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    setSwitcherService(service) {
        this.switcherService = service;
        logger.debug(SVC, 'SwitcherService attaché');
    }

    setMapping(mapping) {
        this.mapping = mapping || {};
        logger.info(SVC, `Mapping mis à jour : ${Object.keys(this.mapping).length} personnages`, Object.keys(this.mapping));
    }

    getMapping() {
        return this.mapping;
    }

    start(config = {}) {
        if (this.process) {
            logger.warn(SVC, 'Processus déjà actif — arrêt avant redémarrage');
            this.stop();
        }

        // Vérification des pré-requis
        const reqs = checkRequirements();
        if (!reqs.tsharkInstalled) {
            logger.error(SVC, 'tshark n\'est pas installé sur le système');
            throw new Error('tshark n\'est pas installé. Veuillez l\'installer via "sudo dnf install wireshark".');
        }
        if (!reqs.tsharkPermissions) {
            logger.warn(SVC, 'tshark semble manquer de permissions pour capturer sur le réseau (setcap manquant)');
            // On continue quand même car le check getcap peut être imprécis selon le chemin
        }

        this.initiativeOrder = [];
        this.lastCharId = null;

        if (config.interface) this.interface = config.interface;
        if (config.mapping) this.mapping = config.mapping;

        logger.info(SVC, `Démarrage sur l'interface "${this.interface}"`, {
            interface: this.interface,
            mappingSize: Object.keys(this.mapping).length,
        });

        const args = ['-i', this.interface, '-f', 'tcp port 5555', '-l', '-T', 'fields', '-e', 'data'];
        logger.debug(SVC, `Spawn tshark avec args : ${args.join(' ')}`);

        try {
            this.process = spawn('tshark', args);
            logger.info(SVC, `tshark démarré (PID ${this.process.pid})`);
        } catch (e) {
            logger.error(SVC, `Impossible de lancer tshark (problème spawn) : ${e.message}`, { code: e.code });
            throw e;
        }

        this.process.stdout.on('data', (data) => {
            const line = data.toString().trim();
            if (!line) return;

            // tshark avec -T fields -e data peut sortir plusieurs lignes ou des colonnes
            const hexPackets = line.split(/\s+/);
            
            for (const hex of hexPackets) {
                if (hex.includes("6a626c")) {
                    logger.debug(SVC, 'Paquet mapping détecté (6a626c)');
                    this.extractAndUpdateMapping(hex);
                } else if (hex.includes("697977")) {
                    logger.debug(SVC, 'Paquet initiative détecté (697977)');
                    this.processInitiative(hex);
                } else if (hex.includes("6a6163")) {
                    logger.debug(SVC, 'Paquet changement de tour détecté (6a6163)');
                    this.processTurn(hex);
                }
            }
        });

        this.process.stderr.on('data', (data) => {
            const msg = data.toString().trim();
            if (msg) {
                // Certains messages tshark sont informatifs (ex: Capturing on 'any')
                if (msg.toLowerCase().includes('error') || msg.toLowerCase().includes('denied') || msg.toLowerCase().includes('permission')) {
                    logger.error(SVC, `tshark stderr : ${msg}`);
                } else {
                    logger.warn(SVC, `tshark stderr : ${msg}`);
                }
            }
        });

        this.process.on('error', (err) => {
            logger.error(SVC, `Erreur processus tshark : ${err.message}`, { code: err.code });
        });

        this.process.on('exit', (code, signal) => {
            logger.info(SVC, `Processus tshark arrêté`, { code, signal });
            this.process = null;
        });
    }

    stop() {
        if (this.process) {
            logger.info(SVC, 'Arrêt du moteur (SIGTERM)...');
            this.process.kill('SIGTERM');
            this.process = null;
        }
        this.initiativeOrder = [];
        this.lastCharId = null;
        logger.info(SVC, 'Service stoppé et état réinitialisé');
    }

    extractAndUpdateMapping(line) {
        const idx = line.indexOf("6a626c");
        if (idx === -1) return;

        const segment = line.substring(idx);
        const regex = /18(e082[a-f0-9]{6})22[0-9a-f]{2}0a[0-9a-f]{2}([0-9a-f]+?)20/g;
        let match;
        let updated = false;

        while ((match = regex.exec(segment)) !== null) {
            const id = match[1];
            const name = Buffer.from(match[2], 'hex').toString('utf8').trim();

            if (name.length > 0 && this.mapping[id] !== name) {
                this.mapping[id] = name;
                updated = true;
                logger.info(SVC, `Nouveau personnage détecté : ${id} -> "${name}"`);
            }
        }

        if (updated && this.mainWindow && !this.mainWindow.isDestroyed()) {
            logger.debug(SVC, 'Envoi mapping-updated au renderer');
            this.mainWindow.webContents.send('autofocus:mapping-updated', this.mapping);
        }
    }

    processInitiative(line) {
        const idx = line.indexOf("697977");
        if (idx === -1) return;

        const segment = line.substring(idx);
        const ids = [];
        const regex = /0a0608(e082[a-f0-9]{6})/g;
        let match;

        while ((match = regex.exec(segment)) !== null) {
            if (this.mapping[match[1]] && !ids.includes(match[1])) {
                ids.push(match[1]);
            }
        }

        if (ids.length > 0) {
            const orderStr = ids.join(",");
            const currentStr = this.initiativeOrder.join(",");
            if (orderStr !== currentStr) {
                this.initiativeOrder = ids;
                this.lastCharId = null;
                const orderNames = ids.map(id => this.mapping[id]).join(' -> ');
                logger.info(SVC, `Ordre d'initiative mis à jour : ${orderNames}`, { ids });

                if (this.mainWindow && !this.mainWindow.isDestroyed()) {
                    this.mainWindow.webContents.send('autofocus:initiative-updated', this.initiativeOrder);
                }
            } else {
                logger.debug(SVC, 'Ordre initiative inchangé, ignoré');
            }
        } else {
            logger.warn(SVC, 'Paquet initiative reçu mais aucun ID connu trouvé (mapping incomplet ?)');
        }
    }

    processTurn(line) {
        if (this.initiativeOrder.length === 0) {
            logger.warn(SVC, 'Paquet tour reçu mais ordre initiative vide — ignoré');
            return;
        }

        const idxJ = line.indexOf("6a6163");
        if (idxJ === -1) return;

        const match = line.substring(idxJ).match(/e082[a-f0-9]{6}/);
        const charId = match ? match[0] : null;

        if (!charId) {
            logger.warn(SVC, 'Paquet tour reçu mais aucun charId extrait');
            return;
        }

        if (!this.mapping[charId]) {
            logger.warn(SVC, `charId "${charId}" inconnu dans le mapping — ignoré`);
            return;
        }

        if (charId === this.lastCharId) {
            logger.debug(SVC, `Tour de "${this.mapping[charId]}" déjà traité — ignoré (doublon)`);
            return;
        }

        this.lastCharId = charId;

        const idx = this.initiativeOrder.indexOf(charId);
        if (idx === -1) {
            logger.warn(SVC, `charId "${charId}" (${this.mapping[charId]}) absent de l'ordre initiative`);
            return;
        }

        const nextId = this.initiativeOrder[(idx + 1) % this.initiativeOrder.length];
        const windowName = this.mapping[nextId];

        logger.info(SVC, `Tour de "${this.mapping[charId]}" → focus sur "${windowName}" dans 150ms`);

        setTimeout(() => {
            this.focusByName(windowName);

            if (this.switcherService) {
                try {
                    const wid = execSync(`xdotool search --name "^${windowName}$"`).toString().trim().split('\n')[0];
                    if (wid) {
                        logger.info(SVC, `SwitcherService.focusWindow("${windowName}", wid=${wid})`);
                        this.switcherService.focusWindow(wid);

                        const { getSwitcherWindow } = require('../windows/dofusSwitcher.cjs');
                        const swin = getSwitcherWindow();
                        if (swin && !swin.isDestroyed()) {
                            swin.webContents.send('switcher:current-changed', wid);
                        }
                    } else {
                        logger.warn(SVC, `xdotool n'a trouvé aucune fenêtre pour "${windowName}"`);
                    }
                } catch (e) {
                    logger.error(SVC, `Erreur xdotool lors du focus SwitcherService : ${e.message}`);
                }
            }
        }, 150);
    }

    focusByName(name) {
        try {
            const wid = execSync(`xdotool search --name "^${name}$"`).toString().trim().split('\n')[0];
            if (wid) {
                execSync(`xdotool windowactivate ${wid}`);
                logger.info(SVC, `xdotool windowactivate "${name}" (wid=${wid}) OK`);
            } else {
                logger.warn(SVC, `Aucune fenêtre trouvée pour "${name}" via xdotool`);
            }
        } catch (e) {
            logger.error(SVC, `focusByName("${name}") — erreur xdotool : ${e.message}`);
        }
    }
}

module.exports = new AutofocusService();
