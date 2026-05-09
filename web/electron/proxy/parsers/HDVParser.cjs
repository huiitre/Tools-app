class HDVParser {
    /**
     * Parse les prix d'un item (EHl)
     */
    parsePrices(message) {
        const segments = message.split('|');
        if (segments.length < 2) return null;
        
        const itemIdMatch = segments[0].match(/EHl(\d+)/);
        if (!itemIdMatch) return null;
        
        const itemId = parseInt(itemIdMatch[1]);
        const allInstances = [];

        for (let i = 1; i < segments.length; i++) {
            const fields = segments[i].split(';');
            if (fields.length < 5) continue;

            const cleanValue = (val) => {
                if (!val) return 0;
                const beforeComma = val.split(',')[0];
                const digitsOnly = beforeComma.replace(/\D/g, '');
                const num = parseInt(digitsOnly);
                return isNaN(num) ? 0 : num;
            };

            allInstances.push({
                itemId: itemId,
                instanceId: fields[0].replace(/\D/g, ''),
                p1: cleanValue(fields[2]),
                p10: cleanValue(fields[3]),
                p100: cleanValue(fields[4]),
                stats: fields[1]
            });
        }
        return { type: 'prices', data: allInstances };
    }

    /**
     * Parse la liste des IDs d'une catégorie (EHL)
     */
    parseCategory(message) {
        const content = message.split('|')[1];
        if (!content) return null;
        
        const ids = content.split(';').filter(id => id.length > 0).map(id => parseInt(id));
        return { type: 'category', data: ids };
    }
}

module.exports = new HDVParser();
