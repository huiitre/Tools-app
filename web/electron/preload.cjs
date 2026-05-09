const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electron', {
  onUpdateAvailable: (callback) => {
    ipcRenderer.on('update-available', callback)
  },
  applyUpdate: () => {
    ipcRenderer.send('apply-update')
  },
  openSwitcher: () => {
    ipcRenderer.invoke('switcher:open')
  },
  onSwitcherClosed: (callback) => ipcRenderer.on('switcher:closed', () => callback()),
  offSwitcherClosed: () => ipcRenderer.removeAllListeners('switcher:closed'),

  // Proxy
  startProxy: (config) => ipcRenderer.invoke('proxy:start', config),
  stopProxy: () => ipcRenderer.invoke('proxy:stop'),
  updateProxyModules: (modules) => ipcRenderer.send('proxy:update-modules', modules),
  onProxyStatus: (callback) => ipcRenderer.on('proxy:status', (event, status) => callback(status)),
  onProxyHdvPrices: (callback) => ipcRenderer.on('proxy:hdv-prices', (event, data) => callback(data)),
  onProxyHdvCategory: (callback) => ipcRenderer.on('proxy:hdv-category', (event, data) => callback(data)),
  onProxyScanProgress: (callback) => ipcRenderer.on('proxy:scan-progress', (event, data) => callback(data)),
  
  onProxyBankOpened: (callback) => ipcRenderer.on('proxy:bank-opened', (event) => callback()),
  onProxyBankItems: (callback) => ipcRenderer.on('proxy:bank-items', (event, items) => callback(items)),
  onProxyBankKamas: (callback) => ipcRenderer.on('proxy:bank-kamas', (event, kamas) => callback(kamas)),

  // Sniffer (Old)
  startSniffing: (forcedConfig = null) => ipcRenderer.invoke('sniffer:start', forcedConfig),
  stopSniffing: () => ipcRenderer.invoke('sniffer:stop'),
  onSnifferData: (callback) => ipcRenderer.on('sniffer:data', (event, data) => callback(data)),
  detectCandidates: () => ipcRenderer.invoke('sniffer:detect-candidates'),
  getActiveConfig: () => ipcRenderer.invoke('sniffer:get-active-config'),
  updateSnifferModules: (config) => ipcRenderer.invoke('sniffer:update-modules', config),
  checkSnifferRequirements: () => ipcRenderer.invoke('sniffer:check-requirements'),

  // Bank
  startBankSniffing: () => ipcRenderer.invoke('sniffer:bank-start'),
  stopBankSniffing: () => ipcRenderer.invoke('sniffer:bank-stop'),
  onBankItemsUpdate: (callback) => ipcRenderer.on('bank-items-update', (event, items) => callback(items)),
  onBankItemCaptured: (callback) => ipcRenderer.on('bank-item-captured', (event, item) => callback(item)),
  onBankFullDump: (callback) => ipcRenderer.on('bank-full-dump', (event) => callback()),

  // Autofocus
  startAutofocus: (config) => ipcRenderer.invoke('autofocus:start', config),
  stopAutofocus: () => ipcRenderer.invoke('autofocus:stop'),
  setAutofocusMapping: (mapping) => ipcRenderer.invoke('autofocus:set-mapping', mapping),
  onAutofocusMappingUpdated: (callback) => ipcRenderer.on('autofocus:mapping-updated', (event, mapping) => callback(mapping)),

  // Logs
  getElectronLogs: () => ipcRenderer.invoke('electron:get-logs'),
  clearElectronLogs: () => ipcRenderer.invoke('electron:clear-logs'),
  onElectronLog: (callback) => ipcRenderer.on('electron:log', (_, entry) => callback(entry)),
  offElectronLog: () => ipcRenderer.removeAllListeners('electron:log'),
})
