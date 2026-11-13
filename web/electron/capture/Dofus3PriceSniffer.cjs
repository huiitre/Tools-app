const logger = require('../logger/LoggerService.cjs');
const captureService = require('./Dofus3CaptureService.cjs');
const {
    MESSAGES,
    TYPE_PREFIX,
    readVarint,
    decodeFields,
    decodePackedVarints,
} = require('./AnkamaMessages.cjs');

const SVC = 'Dofus3PriceSniffer';

const MAX_STREAM_BUFFER = 4 * 1024 * 1024;
const MAX_MESSAGE_SIZE = 2 * 1024 * 1024;

// Seuils de reconnaissance de la liste des prix moyens par sa forme : plusieurs milliers
// d'entrées identiques { id, prix }. Le nom du message change à chaque mise à jour du client,
// cette forme-là beaucoup moins.
const PRICE_LIST_MIN_BYTES = 10 * 1024;
const PRICE_LIST_MIN_ENTRIES = 1000;
const PRICE_LIST_MIN_CONFORMITY = 0.9;

// Le prix moyen tombe en un seul message de ~70 Ko, découpé en dizaines de segments TCP : le flux
// serveur → client est recollé avant d'être lu.
class Dofus3PriceSniffer {
    constructor() {
        this.mainWindow = null;
        this.streams = new Map();
        this.knownAverages = new Map();
        this.active = false;
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    start() {
        if (this.active) return;
        captureService.subscribe('prices', (packet) => this._onPacket(packet));
        this.active = true;
        logger.info(SVC, 'Sniffer de prix démarré');
    }

    stop() {
        captureService.unsubscribe('prices');
        this.streams.clear();
        this.active = false;
        logger.info(SVC, 'Sniffer de prix arrêté');
    }

    _onPacket({ stream, fromServer, seq, data }) {
        if (!fromServer) return;

        let s = this.streams.get(stream);
        if (!s) {
            s = { buf: Buffer.alloc(0), nextSeq: null };
            this.streams.set(stream, s);
        }

        if (s.nextSeq !== null) {
            if (seq + data.length <= s.nextSeq) return; // retransmission complète
            if (seq < s.nextSeq) {
                data = data.subarray(s.nextSeq - seq);   // retransmission partielle
            } else if (seq > s.nextSeq) {
                s.buf = Buffer.alloc(0);                 // trou : le message en cours est perdu
            }
        }

        s.nextSeq = Math.max(s.nextSeq ?? 0, seq + data.length);
        s.buf = s.buf.length ? Buffer.concat([s.buf, data]) : Buffer.from(data);

        if (s.buf.length > MAX_STREAM_BUFFER) {
            logger.warn(SVC, `Flux ${stream} : tampon saturé, réinitialisé`);
            s.buf = Buffer.alloc(0);
            return;
        }

        this._drain(s);
    }

    // Lit tous les messages complets du tampon. Format : type.ankama.com/<nom> 12 <len> <valeur>.
    _drain(s) {
        let buf = s.buf;
        for (;;) {
            const start = buf.indexOf(TYPE_PREFIX);
            if (start === -1) {
                buf = buf.subarray(Math.max(0, buf.length - TYPE_PREFIX.length));
                break;
            }

            let i = start + TYPE_PREFIX.length;
            while (i < buf.length && buf[i] >= 0x61 && buf[i] <= 0x7a) i++;
            if (i + 1 >= buf.length) { buf = buf.subarray(start); break; }

            const name = buf.subarray(start + TYPE_PREFIX.length, i).toString();
            if (buf[i] !== 0x12 || name.length === 0) { buf = buf.subarray(start + 1); continue; }

            const len = readVarint(buf, i + 1);
            if (!len) { buf = buf.subarray(start); break; }
            if (len.value > MAX_MESSAGE_SIZE) { buf = buf.subarray(start + 1); continue; }

            const end = len.next + len.value;
            if (end > buf.length) { buf = buf.subarray(start); break; }

            this._onMessage(name, buf.subarray(len.next, end));
            buf = buf.subarray(end);
        }
        s.buf = Buffer.from(buf);
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
