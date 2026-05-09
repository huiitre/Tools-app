const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('switcher', {
  scan: () => ipcRenderer.invoke('switcher:scan'),
  focusNext: () => ipcRenderer.invoke('switcher:focus-next'),
  focusPrev: () => ipcRenderer.invoke('switcher:focus-prev'),
  focusWindow: (windowId) => ipcRenderer.invoke('switcher:focus-window', windowId),
  setEnabled: (windowId, enabled) => ipcRenderer.invoke('switcher:set-enabled', windowId, enabled),
  setOrder: (windowIds) => ipcRenderer.invoke('switcher:set-order', windowIds),
  renameWindow: (windowId, name) => ipcRenderer.invoke('switcher:rename-window', windowId, name),
  getWindows: () => ipcRenderer.invoke('switcher:get-windows'),
  configureHotkeys: (config) => ipcRenderer.invoke('switcher:configure-hotkeys', config),
  startHotkeys: () => ipcRenderer.invoke('switcher:start-hotkeys'),
  stopHotkeys: () => ipcRenderer.invoke('switcher:stop-hotkeys'),

  onWindowsUpdated: (callback) => {
    ipcRenderer.on('switcher:windows-updated', (_, windows) => callback(windows))
  },
  offWindowsUpdated: () => {
    ipcRenderer.removeAllListeners('switcher:windows-updated')
  },
  captureKey: () => ipcRenderer.invoke('switcher:capture-key'),

  getCurrent: () => ipcRenderer.invoke('switcher:get-current'),

  onCurrentChanged: (callback) => {
    ipcRenderer.on('switcher:current-changed', (_, windowId) => callback(windowId))
  },
  offCurrentChanged: () => {
    ipcRenderer.removeAllListeners('switcher:current-changed')
  },

  // Autofocus
  startAutofocus: (config) => ipcRenderer.invoke('autofocus:start', config),
  stopAutofocus: () => ipcRenderer.invoke('autofocus:stop'),
  setAutofocusMapping: (mapping) => ipcRenderer.invoke('autofocus:set-mapping', mapping),
  onAutofocusMappingUpdated: (callback) => ipcRenderer.on('autofocus:mapping-updated', (event, mapping) => callback(mapping)),

  // Logging
  log: (entry) => ipcRenderer.send('electron:log', entry),
})