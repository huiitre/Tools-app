class BankParser {
    /**
     * Parse le contenu de la banque (Paquet EL)
     */
    parseBankItems(message) {
        const content = message.substring(2); 
        if (!content) return [];

        const items = content.split(/[;|]/);
        const parsedItems = [];

        for (const item of items) {
            if (!item) continue;
            const cleanItem = item.startsWith('O') ? item.substring(1) : item;
            const fields = cleanItem.split('~');
            if (fields.length < 3) continue;

            const instanceId = fields[0];
            const assetId = parseInt(fields[1], 16);
            // La quantité est aussi en HEXADÉCIMAL sur Retro
            const quantity = parseInt(fields[2], 16);

            if (isNaN(assetId)) continue;

            parsedItems.push({
                instanceId: instanceId,
                quantity: isNaN(quantity) ? 1 : quantity,
                assetId: assetId,
                stats: fields[4] || ''
            });
        }
        return parsedItems;
    }

    parseBankKamas(message) {
        // Les kamas peuvent aussi être en hexadécimal selon les serveurs/versions
        const rawKamas = message.substring(2);
        const kamas = parseInt(rawKamas, 16); // On tente l'hex
        return isNaN(kamas) ? parseInt(rawKamas) : kamas; // Fallback décimal
    }
}

module.exports = new BankParser();
