// Lecture des messages Dofus 3 : protobuf enveloppés dans des `Any` (type.ankama.com/<nom>).
// Les noms sont obfusqués et changent à chaque mise à jour du client : ils sont regroupés ici,
// avec la date du relevé. Pour les retrouver, voir la section Autofocus de web/AGENTS.md.

// Relevés le 13/11/2026.
const MESSAGES = {
    mapping: 'jxh',        // ID réseau → nom du personnage (vu à la connexion, plus au changement de map)
    mapActors: 'jnj',      // acteurs présents sur la map au changement de map : { #1 ID, …, nom }
    actorShown: 'jns',     // un acteur apparaît sur la map, même forme qu'une entrée de jnj
    initiative: 'jvu',     // ordre des combattants, monstres compris
    turnEnd: 'jug',        // fin de tour d'un combattant
    turnStart: 'jvj',      // début de tour d'un combattant (ID négatif pour monstres et invocations)
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

// Champ texte protobuf `<tag> <len> <nom>` contenant un nom de personnage Dofus (lettre initiale,
// lettres, chiffres, tirets). Le numéro de champ n'est pas figé (champ 6 de `ksb` en 3.7.13) : seul
// le type « texte » (tag ...010) et une longueur égale au nom sont exigés.
const NAME_FIELD = /([\x0a\x12\x1a\x22\x2a\x32\x3a\x42\x4a\x52])([\x02-\x14])([A-Za-z][A-Za-z0-9-]{1,19})/g;

function extractCharacterName(value) {
    const text = value.toString('latin1');
    for (const match of text.matchAll(NAME_FIELD)) {
        if (match[2].charCodeAt(0) === match[3].length) return match[3];
    }
    return null;
}

function encodeVarintHex(value) {
    const bytes = [];
    let v = value;
    do {
        let b = v % 128;
        v = Math.floor(v / 128);
        if (v > 0) b |= 0x80;
        bytes.push(b);
    } while (v > 0);
    return Buffer.from(bytes).toString('hex');
}

// Premier champ texte (parcours en profondeur) ressemblant à un nom de personnage.
function findCharacterName(buf, depth = 0) {
    const fields = decodeFields(buf);
    if (!fields || depth > 5) return null;
    for (const f of fields) {
        if (f.wire !== 2) continue;
        const text = f.value.toString('latin1');
        if (/^[A-Za-z][A-Za-z0-9-]{1,19}$/.test(text)) return text;
        const nested = findCharacterName(f.value, depth + 1);
        if (nested) return nested;
    }
    return null;
}

// Acteurs d'une map (jnj) ou acteur qui apparaît (jns) : chaque entrée porte l'ID en champ 1 et le
// nom plus profond. Seuls les personnages joueurs (ID e082…) sont retenus.
function extractActors(value) {
    const actors = [];
    for (const entry of decodeFields(value) ?? []) {
        if (entry.wire !== 2) continue;
        const fields = decodeFields(entry.value);
        const idField = fields?.find(f => f.field === 1 && f.wire === 0);
        if (!idField) continue;
        const id = encodeVarintHex(idField.value);
        if (!/^e082[0-9a-f]{6}$/.test(id)) continue;
        const name = findCharacterName(entry.value);
        if (name) actors.push({ id, name });
    }
    return actors;
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
    extractCharacterName,
    extractActors,
};
