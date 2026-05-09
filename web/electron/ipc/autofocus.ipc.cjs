const { ipcMain } = require('electron');
const autofocusService = require('../sniffer/AutofocusService.cjs');
const logger = require('../logger/LoggerService.cjs');

function registerAutofocusIpc(switcherService) {
    autofocusService.setSwitcherService(switcherService);

    ipcMain.handle('autofocus:start', async (_, config) => {
        logger.info('AutofocusIpc', 'Démarrage autofocus demandé', config);
        try {
            autofocusService.start(config);
            return { success: true };
        } catch (e) {
            logger.error('AutofocusIpc', `Erreur au démarrage : ${e.message}`);
            return { success: false, error: e.message };
        }
    });

    ipcMain.handle('autofocus:stop', async () => {
        logger.info('AutofocusIpc', 'Arrêt autofocus demandé');
        autofocusService.stop();
        return { success: true };
    });

    ipcMain.handle('autofocus:set-mapping', async (_, mapping) => {
        logger.info('AutofocusIpc', `set-mapping : ${Object.keys(mapping).length} entrées`);
        autofocusService.setMapping(mapping);
        return { success: true };
    });

    ipcMain.handle('autofocus:get-mapping', async () => {
        return autofocusService.getMapping();
    });
}

module.exports = { registerAutofocusIpc };
