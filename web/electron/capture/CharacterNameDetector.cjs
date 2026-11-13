const { spawnSync } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const captureService = require('./Dofus3CaptureService.cjs');
const { MESSAGES, extractCharacterName } = require('./AnkamaMessages.cjs');
const { StreamReassembler } = require('./StreamReassembler.cjs');

const SVC = 'CharacterNameDetector';

// Détecte le personnage de chaque connexion au serveur de jeu (message reçu à la connexion, qui ne
// contient que ce personnage) et le relie au process Dofus par le port local de la connexion.
class CharacterNameDetector {
    constructor() {
        this.portByStream = new Map();
        this.reassembler = new StreamReassembler((name, value, stream) => this._onMessage(name, value, stream));
        this.onDetected = null;
        this.active = false;
    }

    // onDetected({ pid, name })
    start(onDetected) {
        this.onDetected = onDetected;
        if (this.active) return;
        captureService.subscribe('names', (packet) => {
            this.portByStream.set(packet.stream, packet.clientPort);
            this.reassembler.push(packet);
        });
        this.active = true;
        logger.info(SVC, 'Détection des personnages démarrée');
    }

    stop() {
        captureService.unsubscribe('names');
        this.reassembler.reset();
        this.portByStream.clear();
        this.active = false;
        logger.info(SVC, 'Détection des personnages arrêtée');
    }

    _onMessage(name, value, stream) {
        if (name !== MESSAGES.characterSelected) return;

        const characterName = extractCharacterName(value);
        if (!characterName) {
            logger.warn(SVC, `Message ${MESSAGES.characterSelected} sans nom lisible : sa structure a peut-être changé`);
            return;
        }

        const port = this.portByStream.get(stream);
        const pid = port ? pidForLocalPort(port) : null;
        logger.info(SVC, `Personnage connecté : "${characterName}"`, { stream, port, pid });

        if (pid && this.onDetected) this.onDetected({ pid, name: characterName });
    }
}

// Process propriétaire de la connexion TCP qui part de ce port local.
function pidForLocalPort(port) {
    const res = spawnSync('ss', ['-tpnH', 'sport', '=', `:${port}`], { encoding: 'utf8' });
    const match = res.stdout?.match(/pid=(\d+)/);
    return match ? Number(match[1]) : null;
}

module.exports = new CharacterNameDetector();
