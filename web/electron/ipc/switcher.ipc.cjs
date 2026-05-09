const { ipcMain } = require('electron')
const { execSync } = require('child_process')
const { SwitcherService } = require('../switcher/SwitcherService.cjs')
const { HotkeyManager } = require('../switcher/HotkeyManager.cjs')
const { getSwitcherWindow } = require('../windows/dofusSwitcher.cjs')

const isWindows = process.platform === 'win32'

const { LinuxWindowFocuser } = isWindows ? {} : require('../switcher/adapters/linux/LinuxWindowFocuser.cjs')
const { LinuxWindowScanner } = isWindows ? {} : require('../switcher/adapters/linux/LinuxWindowScanner.cjs')
const { LinuxCharacterNameResolver } = isWindows ? {} : require('../switcher/adapters/linux/LinuxCharacterNameResolver.cjs')
const { WindowsWindowFocuser } = isWindows ? require('../switcher/adapters/windows/WindowsWindowFocuser.cjs') : {}
const { WindowsWindowScanner } = isWindows ? require('../switcher/adapters/windows/WindowsWindowScanner.cjs') : {}
const { WindowsCharacterNameResolver } = isWindows ? require('../switcher/adapters/windows/WindowsCharacterNameResolver.cjs') : {}

const service = new SwitcherService(
  isWindows ? new WindowsWindowScanner() : new LinuxWindowScanner(),
  isWindows ? new WindowsWindowFocuser() : new LinuxWindowFocuser(),
  isWindows ? new WindowsCharacterNameResolver() : new LinuxCharacterNameResolver(),
)

const hotkeyManager = new HotkeyManager(
  async () => {
    await service.focusPrev()
    const win = getSwitcherWindow()
    if (win && !win.isDestroyed()) {
      win.webContents.send('switcher:current-changed', service.getCurrentWindowId())
    }
  },
  async () => {
    await service.focusNext()
    const win = getSwitcherWindow()
    if (win && !win.isDestroyed()) {
      win.webContents.send('switcher:current-changed', service.getCurrentWindowId())
    }
  },
)

function registerSwitcherIpc() {
  ipcMain.handle('switcher:scan', async () => service.scan())
  ipcMain.handle('switcher:focus-next', async () => service.focusNext())
  ipcMain.handle('switcher:focus-prev', async () => service.focusPrev())
  ipcMain.handle('switcher:focus-window', async (_, windowId) => service.focusWindow(windowId))
  ipcMain.handle('switcher:set-enabled', async (_, windowId, enabled) => service.setEnabled(windowId, enabled))
  ipcMain.handle('switcher:set-order', async (_, windowIds) => service.setOrder(windowIds))
  ipcMain.handle('switcher:rename-window', async (_, windowId, name) => {
    if (!isWindows) {
      try {
        execSync(`xdotool set_window --name "${name}" ${windowId}`)
      } catch {}
    }
    return service.renameWindow(windowId, name)
  })
  ipcMain.handle('switcher:get-windows', async () => service.getWindows())
  ipcMain.handle('switcher:get-current', async () => service.getCurrentWindowId())

  ipcMain.handle('switcher:configure-hotkeys', async (_, config) => {
    hotkeyManager.configure(config)
  })

  ipcMain.handle('switcher:start-hotkeys', async () => {
    hotkeyManager.start()
  })

  ipcMain.handle('switcher:stop-hotkeys', async () => {
    hotkeyManager.stop()
  })

  ipcMain.handle('switcher:capture-key', async () => {
    return new Promise((resolve) => {
      hotkeyManager.captureNextKey((keycode) => {
        resolve(keycode)
      })
    })
  })
}

module.exports = { registerSwitcherIpc, switcherService: service }