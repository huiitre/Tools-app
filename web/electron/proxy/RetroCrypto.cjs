const crypto = require('crypto');

/**
 * Logique de cryptographie historique de Dofus Retro
 * Utilisée pour la signature des paquets critiques (ex: EHP pour le HDV)
 */

const RETRO_SALT = "cC+*#!$pie";
const HASH_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_".split('');

/**
 * Signe un paquet avec le Salt Ankama
 * @param {string} packetData - Le contenu du paquet à signer (ex: "EHP123")
 * @returns {string} Hash MD5 en hexadécimal
 */
function signPacket(packetData) {
    const hash = crypto.createHash('md5');
    hash.update(packetData + RETRO_SALT);
    return hash.digest('hex');
}

/**
 * Chiffre des données (historiquement utilisé pour les mots de passe/tickets)
 * @param {string} data 
 * @param {string} key 
 */
function encryptRetroData(data, key = RETRO_SALT) {
    let crypted = "#1";
    
    for (let i = 0; i < data.length; i++) {
        let ch = data.charCodeAt(i);
        let keyChar = key.charCodeAt(i % key.length);
        
        let part1 = Math.floor(ch / 16);
        let part2 = ch % 16;
        
        let index1 = (part1 + keyChar % HASH_CHARS.length) % HASH_CHARS.length;
        let index2 = (part2 + keyChar % HASH_CHARS.length) % HASH_CHARS.length;
        
        crypted += HASH_CHARS[index1] + HASH_CHARS[index2];
    }
    
    return crypted;
}

module.exports = {
    RETRO_SALT,
    signPacket,
    encryptRetroData
};
