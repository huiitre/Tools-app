const { Buffer } = require('buffer');

class BankSniffer {
    constructor(mainWindow) {
        this.mainWindow = mainWindow;
        this.isRecording = false;
        this.allItems = new Set();
        this.streamBuf = '';
        // Ta Regex validée qui chope les items à la volée
        this.itemRegex = /O[0-9a-f]+~[0-9a-f]+~[0-9a-f]+~[^;|\0]*/g;
    }

    start() {
        this.allItems.clear();
        this.streamBuf = '';
        this.isRecording = true;
        console.log('[BankSniffer] Capture démarrée');
    }

    stop() {
        this.isRecording = false;
        const result = Array.from(this.allItems);
        console.log(`[BankSniffer] Capture stoppée : ${result.length} instances`);
        return result;
    }

    handlePacket(buffer, offset, nbytes) {
        if (!this.isRecording) return;

        // Utilisation de latin1 pour préserver les octets bruts des stats
        const chunk = buffer.toString('latin1', offset, nbytes);
        this.streamBuf += chunk;

        let match;
        let foundNew = false;

        // On réinitialise l'index de la regex à chaque fois
        this.itemRegex.lastIndex = 0;

        while ((match = this.itemRegex.exec(this.streamBuf)) !== null) {
            const item = match[0];
            // On vérifie que c'est bien un item (présence de tilde) et qu'on ne l'a pas déjà
            if (item.includes('~') && !this.allItems.has(item)) {
                this.allItems.add(item);
                foundNew = true;
            }
        }

        // Nettoyage du buffer pour éviter l'explosion mémoire tout en gardant le dernier fragment
        const lastO = this.streamBuf.lastIndexOf(';O');
        if (lastO !== -1) {
            this.streamBuf = this.streamBuf.substring(lastO + 1);
        } else if (this.streamBuf.length > 5000) {
            this.streamBuf = this.streamBuf.substring(this.streamBuf.length - 1000);
        }

        // Si on a trouvé des nouveaux items, on push au Frontend immédiatement
        if (foundNew) {
            this.mainWindow.webContents.send('bank-items-update', Array.from(this.allItems));
        }
    }
}

module.exports = BankSniffer;