const { ipcMain } = require('electron');
const snifferService = require('../sniffer/SnifferService.cjs');
const { checkRequirements } = require('../sniffer/SnifferDoctor.cjs');
const { detectPotentialConnections } = require('../sniffer/SnifferDetector.cjs');

function registerSnifferIpc() {
    ipcMain.handle('sniffer:check-requirements', async () => {
        try {
            return checkRequirements();
        } catch (error) {
            return { tcpdumpInstalled: false, hasPermissions: false, error: error.message };
        }
    });

    ipcMain.handle('sniffer:detect-candidates', async () => {
        return detectPotentialConnections();
    });

    ipcMain.handle('sniffer:get-active-config', async () => {
        return snifferService.getActiveConfig();
    });

    ipcMain.handle('sniffer:update-modules', async (event, config) => {
        snifferService.updateModules(config);
        return { success: true };
    });

    ipcMain.handle('sniffer:start', async (event, forcedConfig = null) => {
        try {
            snifferService.start(forcedConfig);
            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    });

    ipcMain.handle('sniffer:stop', () => {
        snifferService.stop();
        return { success: true };
    });

    // Ces handles deviennent obsolètes mais on les garde pour la compatibilité si besoin
    ipcMain.handle('sniffer:bank-start', async () => {
        snifferService.updateModules({ bank: true });
        return { success: true };
    });

    ipcMain.handle('sniffer:bank-stop', async () => {
        snifferService.updateModules({ bank: false });
        return { success: true };
    });
}

module.exports = { registerSnifferIpc };
