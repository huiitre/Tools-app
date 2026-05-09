const net = require('net');
const EventEmitter = require('events');
const crypto = require('crypto');
const logger = require('../logger/LoggerService.cjs');

const SVC = 'ProxySession';

class ProxySession extends EventEmitter {
    constructor(clientSocket, remoteHost, remotePort) {
        super();
        this.clientSocket = clientSocket;
        this.remoteHost = remoteHost;
        this.remotePort = remotePort;
        this.serverSocket = null;
        this.serverBuffer = "";
        this.lastBlob = "";
        this.itemQueue = [];
        this.autoRequestedIds = new Set();
        this.totalInQueue = 0;
        this.currentProcessed = 0;
        this.isProcessing = false;
        this.isDestroyed = false;
        this.modules = { hdvAuto: true, hdvManual: true, bank: true };
        this._setup();
    }

    setModules(modules) {
        this.modules = modules;
        logger.debug(SVC, `Modules configurés`, modules);
    }

    _setup() {
        this.clientSocket.setNoDelay(true);
        const localPort = 60000 + Math.floor(Math.random() * 100);
        logger.info(SVC, `Connexion vers ${this.remoteHost}:${this.remotePort} (port local ${localPort})`);

        this.serverSocket = net.connect({ host: this.remoteHost, port: this.remotePort, localPort: localPort }, () => {
            this.serverSocket.setNoDelay(true);
            logger.info(SVC, `Connecté au serveur ${this.remoteHost}:${this.remotePort}`);
            this.emit('connected');
        });

        this.serverSocket.on('error', (err) => {
            logger.error(SVC, `Erreur socket serveur : ${err.message}`);
            this.destroy();
        });

        this.clientSocket.on('data', (data) => {
            if (this.isDestroyed) return;
            const raw = data.toString('latin1');
            const sigMatch = raw.match(/(Ã¹.*?Ã¹)/);
            if (sigMatch) this.lastBlob = sigMatch[1].replace(/Ã¹/g, '').substring(24);

            if (raw.includes('EV')) {
                logger.debug(SVC, 'EV reçu — reset queue de scan');
                this.itemQueue = [];
                this.autoRequestedIds.clear();
                this.totalInQueue = 0;
                this.currentProcessed = 0;
            }
            if (this.serverSocket.writable) this.serverSocket.write(data);
        });

        this.serverSocket.on('data', (data) => {
            if (this.isDestroyed) return;
            this.serverBuffer += data.toString('latin1');
            let messages = this.serverBuffer.split('\0');
            this.serverBuffer = messages.pop();

            for (let rawMessage of messages) {
                if (!rawMessage) continue;
                const cleanMsg = rawMessage.replace(/^Ã¹.*?Ã¹/, '');

                this.emit('message', cleanMsg);

                if (cleanMsg.startsWith('AYK')) continue;

                if (cleanMsg.startsWith('EHL')) {
                    if (this.modules.hdvAuto) {
                        logger.info(SVC, `EHL reçu — déclenchement scan auto HDV`);
                        this._handleCategoryMessage(cleanMsg);
                    }
                }

                if (cleanMsg.startsWith('EHl')) {
                    const idMatch = cleanMsg.match(/EHl(\d+)/);
                    const id = idMatch ? parseInt(idMatch[1]) : null;
                    const wasRequestedByAuto = id && this.autoRequestedIds.has(id);
                    if (wasRequestedByAuto) {
                        logger.debug(SVC, `EHl${id} reçu (auto-scan)`);
                        this.autoRequestedIds.delete(id);
                    }
                }

                if (this.clientSocket.writable) {
                    this.clientSocket.write(rawMessage + '\0', 'latin1');
                }
            }
        });

        const onClose = () => this.destroy();
        this.clientSocket.on('error', (err) => {
            logger.warn(SVC, `Erreur socket client : ${err.message}`);
            onClose();
        });
        this.clientSocket.on('close', onClose);
        this.serverSocket.on('close', onClose);
    }

    _handleCategoryMessage(message) {
        const content = message.split('|')[1];
        if (!content) return;
        const ids = content.split(';').filter(id => id.length > 0).map(id => parseInt(id));
        logger.info(SVC, `EHL catégorie : ${ids.length} items à scanner`);
        this.itemQueue = ids;
        this.totalInQueue = ids.length;
        this.currentProcessed = 0;
        this._processQueue();
    }

    async _processQueue() {
        if (this.isProcessing || this.itemQueue.length === 0) return;
        this.isProcessing = true;
        logger.info(SVC, `Démarrage scan auto HDV : ${this.itemQueue.length} items`);
        while (this.itemQueue.length > 0) {
            if (this.isDestroyed || !this.modules.hdvAuto) break;
            if (!this.lastBlob) { await new Promise(r => setTimeout(r, 1000)); continue; }
            const id = this.itemQueue.shift();
            this.autoRequestedIds.add(id);
            const packetContent = `EHl${id}`;
            const hash = crypto.createHash('md5').update(packetContent + "cC+*#!$pie").digest('hex');
            const b64Hash = Buffer.from(hash, 'hex').toString('base64');
            const forgedPacket = `Ã¹${b64Hash}${this.lastBlob}Ã¹${packetContent}\n\0`;
            if (this.serverSocket.writable) {
                this.serverSocket.write(forgedPacket, 'latin1');
                const progress = { current: ++this.currentProcessed, total: this.totalInQueue, remaining: this.itemQueue.length };
                this.emit('scan-progress', progress);
                logger.debug(SVC, `Scan HDV : item ${id} (${progress.current}/${progress.total})`);
            }
            await new Promise(r => setTimeout(r, 200));
        }
        logger.info(SVC, `Scan auto HDV terminé : ${this.currentProcessed}/${this.totalInQueue} items traités`);
        this.isProcessing = false;
    }

    injectRaw(packetContent) {
        if (this.clientSocket.writable) this.clientSocket.write(packetContent + '\0', 'latin1');
    }

    destroy() {
        if (this.isDestroyed) return;
        this.isDestroyed = true;
        this.itemQueue = [];
        this.autoRequestedIds.clear();
        logger.info(SVC, `Session détruite (${this.remoteHost}:${this.remotePort})`);
        if (this.clientSocket) this.clientSocket.destroy();
        if (this.serverSocket) this.serverSocket.destroy();
        this.emit('disconnected');
    }
}

module.exports = ProxySession;
