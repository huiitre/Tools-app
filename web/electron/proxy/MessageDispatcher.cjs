const hdvParser = require('./parsers/HDVParser.cjs');
const bankParser = require('./parsers/BankParser.cjs');

class MessageDispatcher {
    constructor() {
        this.mainWindow = null;
    }

    setMainWindow(window) {
        this.mainWindow = window;
    }

    dispatch(message, session) {
        const prefix = message.substring(0, 3);
        const prefix2 = message.substring(0, 2);
        let result = null;

        // --- HDV ---
        if (prefix === 'EHl') {
            result = hdvParser.parsePrices(message);
            if (result && this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('proxy:hdv-prices', result.data);
            }
            return;
        }
        
        if (prefix === 'EHL') {
            result = hdvParser.parseCategory(message);
            if (result && this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('proxy:hdv-category', result.data);
            }
            return;
        }

        // --- BANQUE ---
        if (!session.modules.bank) return; // Sécurité module

        // Liste des items (EL)
        if (prefix2 === 'EL') {
            const items = bankParser.parseBankItems(message);
            if (items.length > 0 && this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('proxy:bank-items', items);
            }
        }

        // Kamas (EK)
        if (prefix2 === 'EK') {
            const kamas = bankParser.parseBankKamas(message);
            if (this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('proxy:bank-kamas', kamas);
            }
        }
        
        // Ouverture Banque (ECK16)
        if (message.startsWith('ECK16')) {
            if (this.mainWindow && !this.mainWindow.isDestroyed()) {
                this.mainWindow.webContents.send('proxy:bank-opened');
            }
        }
    }
}

module.exports = new MessageDispatcher();
