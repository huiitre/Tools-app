const { spawn } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const { checkRequirements } = require('../sniffer/SnifferDoctor.cjs');

const SVC = 'Dofus3Capture';

// Serveur de jeu Dofus 3. Écoute passive : on lit le trafic, on n'intercepte rien.
const GAME_PORT = 5555;

// tcp.payload et non data : quand une heuristique de tshark croit reconnaître un autre protocole
// dans un segment, `data` reste vide et le message en cours de réception est perdu.
const FIELDS = ['tcp.stream', 'tcp.srcport', 'tcp.seq', 'tcp.flags.syn', 'tcp.flags.ack', 'tcp.payload'];

const isTrue = (v) => v === '1' || v === 'True';

// Une seule capture tshark, partagée par l'autofocus et le sniffer de prix : elle tourne tant
// qu'au moins l'un des deux est abonné.
class Dofus3CaptureService {
    constructor() {
        this.process = null;
        this.consumers = new Map();
        this.mainWindow = null;
        this.lineBuffer = '';
        this.status = {
            running: false,
            consumers: [],
            requirements: null,
            error: null,
            lastConnectionAt: null,
        };
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    getStatus() {
        return { ...this.status, consumers: [...this.consumers.keys()] };
    }

    // Les champs propres à un consommateur (prix reçus, etc.) passent par ici pour rester dans
    // un seul objet d'état côté interface.
    patchStatus(patch) {
        Object.assign(this.status, patch);
        this._emitStatus();
    }

    checkRequirements() {
        const reqs = checkRequirements();
        this.status.requirements = {
            tsharkInstalled: reqs.tsharkInstalled,
            tsharkPermissions: reqs.tsharkPermissions,
        };
        this._emitStatus();
        return this.status.requirements;
    }

    // handler({ stream, fromServer, seq, data }) — data est un Buffer, jamais vide.
    subscribe(name, handler) {
        const reqs = this.checkRequirements();
        if (!reqs.tsharkInstalled) {
            throw new Error('tshark n\'est pas installé (paquet wireshark ou tshark).');
        }

        this.consumers.set(name, handler);
        logger.info(SVC, `Abonnement "${name}"`, { consumers: [...this.consumers.keys()] });
        this._ensureRunning();
        this._emitStatus();
    }

    unsubscribe(name) {
        if (!this.consumers.delete(name)) return;
        logger.info(SVC, `Désabonnement "${name}"`, { consumers: [...this.consumers.keys()] });
        if (this.consumers.size === 0) this._stopProcess();
        this._emitStatus();
    }

    stopAll() {
        this.consumers.clear();
        this._stopProcess();
        this._emitStatus();
    }

    _ensureRunning() {
        if (this.process) return;

        const args = ['-i', 'any', '-f', `tcp port ${GAME_PORT}`, '-l', '-T', 'fields', '-E', 'separator=|'];
        for (const f of FIELDS) args.push('-e', f);

        logger.info(SVC, `Démarrage tshark sur le port ${GAME_PORT}`);
        this.lineBuffer = '';
        this.status.error = null;

        const proc = spawn('tshark', args);
        this.process = proc;
        this.status.running = true;

        proc.stdout.on('data', (chunk) => {
            this.lineBuffer += chunk.toString();
            const lines = this.lineBuffer.split('\n');
            this.lineBuffer = lines.pop();
            for (const line of lines) this._onLine(line);
        });

        proc.stderr.on('data', (data) => {
            const msg = data.toString().trim();
            if (!msg) return;
            if (/error|denied|permission/i.test(msg)) {
                logger.error(SVC, `tshark : ${msg}`);
                this.status.error = msg;
                this._emitStatus();
            } else {
                logger.debug(SVC, `tshark : ${msg}`);
            }
        });

        proc.on('error', (err) => {
            logger.error(SVC, `Impossible de lancer tshark : ${err.message}`, { code: err.code });
            this.status.error = err.message;
            this._emitStatus();
        });

        proc.on('exit', (code, signal) => {
            logger.info(SVC, 'tshark arrêté', { code, signal });
            if (this.process === proc) {
                this.process = null;
                this.status.running = false;
                this._emitStatus();
            }
        });
    }

    _stopProcess() {
        if (!this.process) return;
        logger.info(SVC, 'Arrêt de tshark');
        const proc = this.process;
        this.process = null;
        this.status.running = false;
        proc.stdout.removeAllListeners('data');
        proc.kill('SIGTERM');

        // Un tshark arrêté puis relancé aussitôt (droits relus au démarrage de la session) a été
        // vu survivre au SIGTERM : deux captures tournaient alors en parallèle.
        setTimeout(() => {
            if (proc.exitCode === null && proc.signalCode === null) {
                logger.warn(SVC, `tshark (PID ${proc.pid}) toujours actif 2 s après SIGTERM, SIGKILL`);
                proc.kill('SIGKILL');
            }
        }, 2000);
    }

    _onLine(line) {
        const [stream, srcPort, seq, syn, ack, hex] = line.trim().split('|');
        if (stream === undefined) return;

        const fromServer = Number(srcPort) === GAME_PORT;

        // SYN du client vers le serveur : une connexion de jeu commence.
        if (isTrue(syn) && !isTrue(ack) && !fromServer) {
            this.status.lastConnectionAt = Date.now();
            logger.info(SVC, `Connexion au serveur de jeu détectée (flux ${stream})`);
            this._emitStatus();
        }

        if (!hex) return;

        const packet = { stream, fromServer, seq: Number(seq), data: Buffer.from(hex, 'hex'), hex };
        for (const [name, handler] of this.consumers) {
            try {
                handler(packet);
            } catch (e) {
                logger.error(SVC, `Erreur dans le consommateur "${name}" : ${e.message}`);
            }
        }
    }

    _emitStatus() {
        if (this.mainWindow && !this.mainWindow.isDestroyed()) {
            this.mainWindow.webContents.send('capture:status', this.getStatus());
        }
    }
}

module.exports = new Dofus3CaptureService();
