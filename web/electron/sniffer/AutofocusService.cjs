const { execSync } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const captureService = require('../capture/Dofus3CaptureService.cjs');
const { MESSAGES, byteAlignedMatches, extractCharacterName, extractActors } = require('../capture/AnkamaMessages.cjs');
const { StreamReassembler } = require('../capture/StreamReassembler.cjs');

const SVC = 'AutofocusService';

// Les noms des messages et la procédure pour les retrouver après une mise à jour du client sont
// dans capture/AnkamaMessages.cjs. Les numéros de champs des motifs ci-dessous peuvent bouger
// avec les noms : à revérifier ensemble.

// Avec plusieurs comptes, chaque message arrive une fois par connexion : un même début de tour
// reçu dans ce délai est un doublon.
const TURN_DUPLICATE_MS = 3000;

// Titres des fenêtres Dofus ouvertes, relus au plus toutes les 5 s.
const WINDOW_NAMES_TTL_MS = 5000;

class AutofocusService {
    constructor() {
        this.active = false;
        this.mapping = {};
        this.initiativeOrder = [];
        this.lastTurnStart = null;
        this.mainWindow = null;
        this.switcherService = null;
        // Messages complets uniquement : lus paquet par paquet, un message coupé entre deux
        // segments TCP donnait des noms tronqués (« Gamama » pour « Gamamaru »).
        this.reassembler = new StreamReassembler((name, value) => this._onMessage(name, value));
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
        this.initiativeOrder = [];
        this.lastTurnStart = null;
        this.reassembler.reset();
        if (config.mapping) this.mapping = config.mapping;

        logger.info(SVC, 'Démarrage', { mappingSize: Object.keys(this.mapping).length });

        captureService.subscribe('autofocus', (packet) => this.reassembler.push(packet));
        this.active = true;
    }

    _onMessage(name, value) {
        const hex = value.toString('hex');
        switch (name) {
            case MESSAGES.mapping:
                logger.debug(SVC, `Message mapping détecté (${MESSAGES.mapping})`);
                this.extractAndUpdateMapping(hex);
                break;
            case MESSAGES.characterSelected:
                this.learnFromConnection(value, hex);
                break;
            case MESSAGES.mapActors:
            case MESSAGES.actorShown:
                this.learnFromMap(value);
                break;
            case MESSAGES.initiative:
                logger.debug(SVC, `Message initiative détecté (${MESSAGES.initiative})`);
                this.processInitiative(hex);
                break;
            case MESSAGES.turnStart:
                this.processTurnStart(hex);
                break;
        }
    }

    stop() {
        captureService.unsubscribe('autofocus');
        this.active = false;
        this.initiativeOrder = [];
        this.lastTurnStart = null;
        this.reassembler.reset();
        logger.info(SVC, 'Service stoppé et état réinitialisé');
    }

    _learn(entries) {
        let updated = false;
        for (const { id, name } of entries) {
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

    // Acteurs de la map : on n'apprend que tes personnages (nom d'une fenêtre Dofus ouverte, ou ID déjà
    // connu), pas les autres joueurs présents sur la map.
    learnFromMap(value) {
        const actors = extractActors(value);
        if (actors.length === 0) return;
        const windows = this._windowNames();
        this._learn(actors.filter(a => this.mapping[a.id] || windows.has(a.name)));
    }

    _windowNames() {
        const now = Date.now();
        if (this._windowNamesCache && now - this._windowNamesCache.at < WINDOW_NAMES_TTL_MS) {
            return this._windowNamesCache.names;
        }
        let names = new Set();
        try {
            const out = execSync('xdotool search --class Dofus.x64 getwindowname %@').toString();
            names = new Set(out.split('\n').map(n => n.trim()).filter(Boolean));
        } catch {}
        this._windowNamesCache = { at: now, names };
        return names;
    }

    // Personnage de la connexion : 18 <ID> en tête, puis son nom dans un champ texte.
    learnFromConnection(value, hex) {
        const idMatch = byteAlignedMatches(/18(e082[0-9a-f]{6})/g, hex).next().value;
        const name = extractCharacterName(value);
        if (idMatch && name) this._learn([{ id: idMatch[1], name }]);
    }

    // Une entrée par personnage : 18 <ID> 2a <len> 12 <len nom> <nom utf8>
    extractAndUpdateMapping(segment) {
        const regex = /18(e082[0-9a-f]{6})2a[0-9a-f]{2}12([0-9a-f]{2})/g;
        const entries = [];

        for (const match of byteAlignedMatches(regex, segment)) {
            const length = parseInt(match[2], 16) * 2;
            const nameStart = match.index + match[0].length;
            const nameHex = segment.substr(nameStart, length);
            // Nom incomplet : on n'enregistre jamais un nom tronqué.
            if (nameHex.length !== length) continue;
            entries.push({ id: match[1], name: Buffer.from(nameHex, 'hex').toString('utf8').trim() });
        }

        this._learn(entries);
    }

    // Une entrée par combattant : 12 06 08 <ID>. Les monstres ont des ID négatifs, sans e082.
    processInitiative(segment) {
        const ids = [];
        const regex = /120608(e082[0-9a-f]{6})/g;

        for (const match of byteAlignedMatches(regex, segment)) {
            if (this.mapping[match[1]] && !ids.includes(match[1])) {
                ids.push(match[1]);
            }
        }

        if (ids.length > 0) {
            // À chaque combat, même ordre identique au précédent : l'ordre a pu être changé à la main
            // entre deux combats. Le switcher ignore un ordre déjà en place.
            if (this.switcherService?.applyFightOrder) {
                this.switcherService.applyFightOrder(ids.map(id => this.mapping[id]));
            }

            const orderStr = ids.join(",");
            const currentStr = this.initiativeOrder.join(",");
            if (orderStr !== currentStr) {
                this.initiativeOrder = ids;
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

    // Début de tour : 18 <ID> du combattant. Monstres et invocations ont un ID négatif (sans e082) :
    // on ne touche pas au focus, la fenêtre de l'invocateur reste devant pendant que ses invocations jouent.
    processTurnStart(hex) {
        const match = byteAlignedMatches(/18(e082[0-9a-f]{6})/g, hex).next().value;
        const charId = match ? match[1] : null;

        if (!charId) {
            logger.debug(SVC, 'Début de tour d\'un monstre ou d\'une invocation — focus inchangé');
            return;
        }

        const now = Date.now();
        if (this.lastTurnStart?.id === charId && now - this.lastTurnStart.at < TURN_DUPLICATE_MS) return;
        this.lastTurnStart = { id: charId, at: now };

        const windowName = this.mapping[charId];
        if (!windowName) {
            logger.warn(SVC, `charId "${charId}" inconnu dans le mapping — ignoré`);
            return;
        }

        logger.info(SVC, `Début du tour de "${windowName}" → focus dans 150ms`);
        setTimeout(() => this._focusCharacter(windowName), 150);
    }

    _focusCharacter(windowName) {
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
