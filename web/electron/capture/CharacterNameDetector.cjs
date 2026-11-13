const { spawnSync } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const captureService = require('./Dofus3CaptureService.cjs');
const { MESSAGES } = require('./AnkamaMessages.cjs');
const { StreamReassembler } = require('./StreamReassembler.cjs');

const SVC = 'CharacterNameDetector';

// Champ texte protobuf `<tag> <len> <nom>` contenant un nom de personnage Dofus (lettre initiale,
// lettres, chiffres, tirets). Le numéro de champ n'est pas figé (champ 6 en 3.7.13) : seul le type
// « texte » (tag ...010) et une longueur égale au nom sont exigés.
const NAME_FIELD = /([\x0a\x12\x1a\x22\x2a\x32\x3a\x42\x4a\x52])([\x02-\x14])([A-Za-z][A-Za-z0-9-]{1,19})/g;

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

        const characterName = extractName(value);
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

function extractName(value) {
    const text = value.toString('latin1');
    for (const match of text.matchAll(NAME_FIELD)) {
        if (match[2].charCodeAt(0) === match[3].length) return match[3];
    }
    return null;
}

// Process propriétaire de la connexion TCP qui part de ce port local.
function pidForLocalPort(port) {
    const res = spawnSync('ss', ['-tpnH', 'sport', '=', `:${port}`], { encoding: 'utf8' });
    const match = res.stdout?.match(/pid=(\d+)/);
    return match ? Number(match[1]) : null;
}

module.exports = new CharacterNameDetector();
module.exports.extractName = extractName;
