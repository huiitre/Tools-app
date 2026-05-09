const { BrowserWindow } = require('electron')
const { execSync } = require('child_process')
const path = require('path')
const logger = require('../logger/LoggerService.cjs')

const SVC = 'SwitcherWindow'

let switcherWindow = null

function enforceAlwaysOnTop(win) {
  // Niveau le plus élevé disponible sous Electron
  win.setAlwaysOnTop(true, 'screen-saver')
  // Visible sur tous les bureaux virtuels
  win.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true })

  // Sur Linux, on renforce _NET_WM_STATE_ABOVE via XWayland avec wmctrl ou xdotool
  if (process.platform === 'linux') {
    setTimeout(() => {
      // Tentative 1 : wmctrl
      try {
        execSync('wmctrl -r "Dofus Switcher" -b add,above,sticky')
        logger.info(SVC, 'wmctrl always-on-top appliqué')
        return
      } catch {
        // wmctrl absent
      }

      // Tentative 2 : xdotool windowstate (déjà utilisé dans ce projet)
      try {
        const wid = execSync('xdotool search --name "Dofus Switcher"').toString().trim().split('\n')[0]
        if (wid) {
          execSync(`xdotool windowstate --add ABOVE --add STICKY ${wid}`)
          logger.info(SVC, `xdotool windowstate ABOVE+STICKY appliqué (wid=${wid})`)
        } else {
          logger.warn(SVC, 'xdotool : aucune fenêtre "Dofus Switcher" trouvée')
        }
      } catch (e) {
        logger.warn(SVC, `xdotool windowstate échoué : ${e.message}`)
      }
    }, 500)
  }
}

function createSwitcherWindow() {
  if (switcherWindow && !switcherWindow.isDestroyed()) {
    enforceAlwaysOnTop(switcherWindow)
    switcherWindow.focus()
    return switcherWindow
  }

  switcherWindow = new BrowserWindow({
    width: 360,
    height: 600,
    minWidth: 300,
    minHeight: 400,
    alwaysOnTop: true,
    title: 'Dofus Switcher',
    webPreferences: {
      preload: path.join(__dirname, '..', 'preloads', 'switcher.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  enforceAlwaysOnTop(switcherWindow)
  logger.info(SVC, 'Fenêtre Switcher créée')

  const isDev = process.env.ELECTRON_DEV === 'true'
  if (isDev) {
    switcherWindow.loadURL('http://localhost:5173/#/dofus-switcher')
  } else {
    switcherWindow.loadFile(
      path.join(__dirname, '..', '..', 'dist', 'index.html'),
      { hash: '/dofus-switcher' }
    )
  }

  switcherWindow.webContents.on('did-finish-load', () => {
    switcherWindow.setTitle('Dofus Switcher')
    // Réappliquer après le chargement complet de la page
    enforceAlwaysOnTop(switcherWindow)
    logger.info(SVC, 'did-finish-load — always-on-top réappliqué')
  })

  switcherWindow.on('closed', () => {
    switcherWindow = null
    const { BrowserWindow } = require('electron')
    const mainWindow = BrowserWindow.getAllWindows().find(w => w !== switcherWindow)
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send('switcher:closed')
    }
  })


  return switcherWindow
}

function getSwitcherWindow() {
  return switcherWindow
}

module.exports = { createSwitcherWindow, getSwitcherWindow }