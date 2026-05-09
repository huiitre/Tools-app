const { ipcMain } = require('electron');
const proxyService = require('../proxy/ProxyService.cjs');

function registerProxyIpc() {
    ipcMain.handle('proxy:start', async (event, config) => {
        try {
            return await proxyService.start(config);
        } catch (error) {
            return { success: false, error: error.message };
        }
    });

    ipcMain.handle('proxy:stop', async () => {
        await proxyService.stop();
        return { success: true };
    });

    ipcMain.on('proxy:update-modules', (event, modules) => {
        proxyService.updateModules(modules);
    });
}

module.exports = { registerProxyIpc };
