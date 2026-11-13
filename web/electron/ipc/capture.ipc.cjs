const { ipcMain } = require('electron');
const captureService = require('../capture/Dofus3CaptureService.cjs');
const priceSniffer = require('../capture/Dofus3PriceSniffer.cjs');
const autofocusService = require('../sniffer/AutofocusService.cjs');
const nameDetector = require('../capture/CharacterNameDetector.cjs');
const { switcherService } = require('./switcher.ipc.cjs');
const logger = require('../logger/LoggerService.cjs');

function registerCaptureIpc() {
    ipcMain.handle('capture:get-status', () => captureService.getStatus());

    ipcMain.handle('capture:check-requirements', () => captureService.checkRequirements());

    ipcMain.handle('capture:start-prices', () => {
        try {
            priceSniffer.start();
            return { success: true };
        } catch (e) {
            logger.error('CaptureIpc', `Démarrage du sniffer de prix impossible : ${e.message}`);
            return { success: false, error: e.message };
        }
    });

    ipcMain.handle('capture:stop-prices', () => {
        priceSniffer.stop();
        return { success: true };
    });

    // Détection automatique : suivi des fenêtres Dofus + nom du personnage à sa connexion.
    ipcMain.handle('capture:start-detection', () => {
        try {
            nameDetector.start(({ pid, name }) => switcherService.applyDetectedName(pid, name));
            switcherService.startWatching();
            return { success: true };
        } catch (e) {
            logger.error('CaptureIpc', `Démarrage de la détection impossible : ${e.message}`);
            return { success: false, error: e.message };
        }
    });

    ipcMain.handle('capture:stop-detection', () => {
        nameDetector.stop();
        switcherService.stopWatching();
        return { success: true };
    });

    // Déconnexion du compte Tools : plus rien n'écoute le réseau.
    ipcMain.handle('capture:stop-all', () => {
        priceSniffer.stop();
        autofocusService.stop();
        nameDetector.stop();
        switcherService.stopWatching();
        captureService.stopAll();
        return { success: true };
    });
}

module.exports = { registerCaptureIpc };
