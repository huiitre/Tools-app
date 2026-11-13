const logger = require('../logger/LoggerService.cjs');
const captureService = require('./Dofus3CaptureService.cjs');
const { MESSAGES, decodeFields, decodePackedVarints } = require('./AnkamaMessages.cjs');
const { StreamReassembler } = require('./StreamReassembler.cjs');

const SVC = 'Dofus3PriceSniffer';

// Seuils de reconnaissance de la liste des prix moyens par sa forme : plusieurs milliers
// d'entrées identiques { id, prix }. Le nom du message change à chaque mise à jour du client,
// cette forme-là beaucoup moins.
const PRICE_LIST_MIN_BYTES = 10 * 1024;
const PRICE_LIST_MIN_ENTRIES = 1000;
const PRICE_LIST_MIN_CONFORMITY = 0.9;

// Le prix moyen tombe en un seul message de ~70 Ko, découpé en dizaines de segments TCP : le flux
// serveur → client est recollé (StreamReassembler) avant d'être lu.
class Dofus3PriceSniffer {
    constructor() {
        this.mainWindow = null;
        this.reassembler = new StreamReassembler((name, value) => this._onMessage(name, value));
        this.knownAverages = new Map();
        this.active = false;
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    start() {
        if (this.active) return;
        captureService.subscribe('prices', (packet) => this.reassembler.push(packet));
        this.active = true;
        logger.info(SVC, 'Sniffer de prix démarré');
    }

    stop() {
        captureService.unsubscribe('prices');
        this.reassembler.reset();
        this.active = false;
        logger.info(SVC, 'Sniffer de prix arrêté');
    }

    _onMessage(name, value) {
        if (name === MESSAGES.itemOffers) {
            this._onItemOffers(value);
            return;
        }

        if (value.length >= PRICE_LIST_MIN_BYTES) {
            const entries = decodePriceList(value);
            if (entries) this._onPriceList(name, entries);
        }
    }

    _onPriceList(name, entries) {
        if (name !== MESSAGES.averagePrices) {
            logger.warn(SVC, `Liste de prix reconnue sous le nom "${name}" au lieu de "${MESSAGES.averagePrices}" : le client a été mis à jour`);
        }

        this.knownAverages = new Map(entries.filter(e => e.price !== null).map(e => [e.id, e.price]));
        logger.info(SVC, `Prix moyens reçus : ${entries.length} objets (${this.knownAverages.size} avec un prix)`);

        captureService.patchStatus({ lastPriceListAt: Date.now(), lastPriceListCount: entries.length });
        this._send('capture:price-list', { entries, receivedAt: Date.now() });
    }

    // #1 { #1 id de l'objet, #2 [prix x1, x10, x100, x1000] (0 = aucun lot) }
    _onItemOffers(value) {
        const top = decodeFields(value);
        const inner = top?.find(f => f.field === 1 && f.wire === 2);
        const fields = inner && decodeFields(inner.value);
        const id = fields?.find(f => f.field === 1 && f.wire === 0)?.value;
        const packed = fields?.find(f => f.field === 2 && f.wire === 2);
        const lots = packed && decodePackedVarints(packed.value);

        if (!id || !lots) {
            logger.warn(SVC, `Message ${MESSAGES.itemOffers} illisible : sa structure a peut-être changé`);
            return;
        }

        const offer = {
            itemId: id,
            lots: [0, 1, 2, 3].map(i => lots[i] ?? 0),
            knownAverage: this.knownAverages.get(id) ?? null,
            receivedAt: Date.now(),
        };
        logger.info(SVC, `Prix des lots reçus pour l'objet ${id}`, offer);
        this._send('capture:item-offers', offer);
    }

    _send(channel, payload) {
        if (this.mainWindow && !this.mainWindow.isDestroyed()) {
            this.mainWindow.webContents.send(channel, payload);
        }
    }
}

// Un seul champ répété dont chaque entrée ne contient que des entiers : le plus petit numéro de
// champ est l'id, l'autre le prix (absent pour un objet jamais vendu).
function decodePriceList(value) {
    const top = decodeFields(value);
    if (!top || top.length < PRICE_LIST_MIN_ENTRIES) return null;

    const field = top[0].field;
    if (top.some(f => f.field !== field || f.wire !== 2)) return null;

    const entries = [];
    for (const { value: entry } of top) {
        const sub = decodeFields(entry);
        if (!sub || sub.length === 0 || sub.length > 2 || sub.some(f => f.wire !== 0)) continue;
        const [idField, priceField] = [...sub].sort((a, b) => a.field - b.field);
        entries.push({ id: idField.value, price: priceField ? priceField.value : null });
    }

    if (entries.length < top.length * PRICE_LIST_MIN_CONFORMITY) return null;
    return entries;
}

module.exports = new Dofus3PriceSniffer();
module.exports.decodePriceList = decodePriceList;
