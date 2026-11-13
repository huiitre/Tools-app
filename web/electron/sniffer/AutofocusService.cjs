const { execSync } = require('child_process');
const logger = require('../logger/LoggerService.cjs');
const captureService = require('../capture/Dofus3CaptureService.cjs');
const { MESSAGES, messagesOfType, byteAlignedMatches } = require('../capture/AnkamaMessages.cjs');

const SVC = 'AutofocusService';

// Les noms des messages et la procédure pour les retrouver après une mise à jour du client sont
// dans capture/AnkamaMessages.cjs. Les numéros de champs des motifs ci-dessous peuvent bouger
// avec les noms : à revérifier ensemble.

class AutofocusService {
    constructor() {
        this.active = false;
        this.mapping = {};
        this.initiativeOrder = [];
        this.lastCharId = null;
        this.mainWindow = null;
        this.switcherService = null;
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
        this.lastCharId = null;
        if (config.mapping) this.mapping = config.mapping;

        logger.info(SVC, 'Démarrage', { mappingSize: Object.keys(this.mapping).length });

        // Paquet par paquet, comme avant le partage de la capture : les messages de combat
        // tiennent dans un seul segment TCP.
        captureService.subscribe('autofocus', ({ hex }) => this.handlePacket(hex));
        this.active = true;
    }

    handlePacket(hex) {
        for (const message of messagesOfType(hex, MESSAGES.mapping)) {
            logger.debug(SVC, `Message mapping détecté (${MESSAGES.mapping})`);
            this.extractAndUpdateMapping(message);
        }
        for (const message of messagesOfType(hex, MESSAGES.initiative)) {
            logger.debug(SVC, `Message initiative détecté (${MESSAGES.initiative})`);
            this.processInitiative(message);
        }
        for (const message of messagesOfType(hex, MESSAGES.turnEnd)) {
            logger.debug(SVC, `Message fin de tour détecté (${MESSAGES.turnEnd})`);
            this.processTurn(message);
        }
    }

    stop() {
        captureService.unsubscribe('autofocus');
        this.active = false;
        this.initiativeOrder = [];
        this.lastCharId = null;
        logger.info(SVC, 'Service stoppé et état réinitialisé');
    }

    // Une entrée par personnage : 18 <ID> 2a <len> 12 <len nom> <nom utf8>
    extractAndUpdateMapping(segment) {
        const regex = /18(e082[0-9a-f]{6})2a[0-9a-f]{2}12([0-9a-f]{2})/g;
        let updated = false;

        for (const match of byteAlignedMatches(regex, segment)) {
            const id = match[1];
            const nameStart = match.index + match[0].length;
            const nameHex = segment.substr(nameStart, parseInt(match[2], 16) * 2);
            const name = Buffer.from(nameHex, 'hex').toString('utf8').trim();

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

    // 12 06 10 <ID>. Une fin de tour de monstre n'a pas d'ID e082 : rien à faire.
    processTurn(segment) {
        if (this.initiativeOrder.length === 0) {
            logger.warn(SVC, 'Fin de tour reçue mais ordre initiative vide — ignorée');
            return;
        }

        const match = byteAlignedMatches(/10(e082[0-9a-f]{6})/g, segment).next().value;
        const charId = match ? match[1] : null;

        if (!charId) {
            logger.debug(SVC, 'Fin de tour d\'un combattant non joueur — ignorée');
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
