class LoggerService {
  constructor() {
    this.logs = [];
    this.maxLogs = 2000;
    this.mainWindow = null;
  }

  setMainWindow(win) {
    this.mainWindow = win;
  }

  _log(level, service, message, data) {
    const entry = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
      timestamp: new Date().toISOString(),
      level,
      service,
      message,
      data: data !== undefined ? data : null,
    };

    this.logs.push(entry);
    if (this.logs.length > this.maxLogs) this.logs.shift();

    const prefix = `[${entry.timestamp.substring(11, 23)}] [${service}]`;
    if (level === 'error') console.error(prefix, message, data ?? '');
    else if (level === 'warn') console.warn(prefix, message, data ?? '');
    else console.log(prefix, message, data ?? '');

    if (this.mainWindow && !this.mainWindow.isDestroyed()) {
      this.mainWindow.webContents.send('electron:log', entry);
    }
  }

  info(service, message, data) { this._log('info', service, message, data); }
  warn(service, message, data) { this._log('warn', service, message, data); }
  error(service, message, data) { this._log('error', service, message, data); }
  debug(service, message, data) { this._log('debug', service, message, data); }

  getLogs() { return this.logs; }
  clear() { this.logs = []; }
}

module.exports = new LoggerService();
