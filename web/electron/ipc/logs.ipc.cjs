const { ipcMain } = require('electron');
const logger = require('../logger/LoggerService.cjs');

function registerLogsIpc() {
  ipcMain.handle('electron:get-logs', () => logger.getLogs());
  ipcMain.handle('electron:clear-logs', () => {
    logger.clear();
    return { success: true };
  });
}

module.exports = { registerLogsIpc };
