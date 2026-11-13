// Lecture des messages Dofus 3 : protobuf enveloppés dans des `Any` (type.ankama.com/<nom>).
// Les noms sont obfusqués et changent à chaque mise à jour du client : ils sont regroupés ici,
// avec la date du relevé. Pour les retrouver, voir la section Autofocus de web/AGENTS.md.

// Relevés le 13/11/2026.
const MESSAGES = {
    mapping: 'jxh',        // ID réseau → nom du personnage
    initiative: 'jvu',     // ordre des combattants, monstres compris
    turnEnd: 'jug',        // fin de tour d'un combattant
    averagePrices: 'isr',  // prix moyens de tous les objets de l'HDV, reçu à la connexion
    itemOffers: 'jzs',     // prix des lots d'un objet, reçu au clic dans l'HDV
    characterSelected: 'ksb', // personnage de la connexion (ID, date, nom), reçu à sa connexion
};

const TYPE_PREFIX = Buffer.from('type.ankama.com/');
const TYPE_PREFIX_HEX = TYPE_PREFIX.toString('hex');

// Découpe la charge utile hexadécimale d'un paquet en messages du type demandé, chacun borné au
// message suivant pour ne jamais lire l'ID d'un autre message.
function messagesOfType(hex, typeName) {
    const starts = [];
    let i = hex.indexOf(TYPE_PREFIX_HEX);
    while (i !== -1) {
        if (i % 2 === 0) starts.push(i);
        i = hex.indexOf(TYPE_PREFIX_HEX, i + 1);
    }

    const marker = TYPE_PREFIX_HEX + Buffer.from(typeName).toString('hex');
    return starts
        .map((start, n) => hex.substring(start, starts[n + 1] ?? hex.length))
        .filter(segment => segment.startsWith(marker));
}

// Les regex tournent sur une chaîne hexadécimale : seules les positions paires sont des octets.
function* byteAlignedMatches(regex, hex) {
    for (const match of hex.matchAll(regex)) {
        if (match.index % 2 === 0) yield match;
    }
}

// Varint protobuf. Retourne null si le buffer s'arrête au milieu.
function readVarint(buf, offset) {
    let value = 0;
    let shift = 0;
    let i = offset;
    while (i < buf.length) {
        const byte = buf[i++];
        value += (byte & 0x7f) * 2 ** shift;
        if ((byte & 0x80) === 0) return { value, next: i };
        shift += 7;
        if (shift > 63) return null;
    }
    return null;
}

// Champs d'un message protobuf. Retourne null au premier type de champ non géré.
function decodeFields(buf) {
    const fields = [];
    let i = 0;
    while (i < buf.length) {
        const key = readVarint(buf, i);
        if (!key) return null;
        const field = Math.floor(key.value / 8);
        const wire = key.value % 8;
        i = key.next;

        if (wire === 0) {
            const v = readVarint(buf, i);
            if (!v) return null;
            fields.push({ field, wire, value: v.value });
            i = v.next;
        } else if (wire === 2) {
            const len = readVarint(buf, i);
            if (!len || len.next + len.value > buf.length) return null;
            fields.push({ field, wire, value: buf.subarray(len.next, len.next + len.value) });
            i = len.next + len.value;
        } else {
            return null;
        }
    }
    return fields;
}

function decodePackedVarints(buf) {
    const values = [];
    let i = 0;
    while (i < buf.length) {
        const v = readVarint(buf, i);
        if (!v) return null;
        values.push(v.value);
        i = v.next;
    }
    return values;
}

module.exports = {
    MESSAGES,
    TYPE_PREFIX,
    TYPE_PREFIX_HEX,
    messagesOfType,
    byteAlignedMatches,
    readVarint,
    decodeFields,
    decodePackedVarints,
};
