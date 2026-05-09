const { app, BrowserWindow, ipcMain, nativeImage, session } = require('electron')
const { autoUpdater } = require('electron-updater')
const path = require('path')
const { createSwitcherWindow } = require('./windows/dofusSwitcher.cjs')
const { registerSwitcherIpc } = require('./ipc/switcher.ipc.cjs')
const { registerSnifferIpc } = require('./ipc/sniffer.ipc.cjs')
const { registerProxyIpc } = require('./ipc/proxy.ipc.cjs')
const { registerAutofocusIpc } = require('./ipc/autofocus.ipc.cjs')
const { registerLogsIpc } = require('./ipc/logs.ipc.cjs')
const logger = require('./logger/LoggerService.cjs')

function createWindow() {
  const appIcon = nativeImage.createFromPath(path.join(__dirname, 'icon.png'))

  const win = new BrowserWindow({
    width: 1600,
    height: 960,
    icon: appIcon,
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      webviewTag: true,
    }
  })

  win.setIcon(appIcon)

  logger.setMainWindow(win)
  logger.info('Main', 'Fenêtre principale créée')

  // Initialisation du mainWindow sur les services qui en ont besoin
  const autofocusService = require('./sniffer/AutofocusService.cjs')
  const snifferService = require('./sniffer/SnifferService.cjs')
  const proxyService = require('./proxy/ProxyService.cjs')
  autofocusService.setMainWindow(win)
  snifferService.setMainWindow(win)
  proxyService.setMainWindow(win)

  const isDev = process.env.ELECTRON_DEV === 'true'

  if (isDev) {
    win.loadURL('http://localhost:5173')
    win.webContents.openDevTools()
    logger.info('Main', 'Mode DEV — chargement http://localhost:5173')
  } else {
    win.loadFile(path.join(__dirname, '..', 'dist', 'index.html'))
    win.webContents.on('devtools-opened', () => {
      win.webContents.closeDevTools()
    })

    autoUpdater.on('checking-for-update', () => {
      logger.info('Updater', 'Vérification des mises à jour...')
    })

    autoUpdater.on('update-available', (info) => {
      logger.info('Updater', `Mise à jour disponible : ${info.version}`)
      win.webContents.send('update-available')
    })

    autoUpdater.on('update-not-available', (info) => {
      logger.info('Updater', `Pas de mise à jour. Version actuelle : ${info.version}`)
    })

    autoUpdater.on('error', (err) => {
      logger.error('Updater', `Erreur : ${err.message}`)
    })

    autoUpdater.on('download-progress', (progress) => {
      logger.info('Updater', `Téléchargement : ${Math.round(progress.percent)}%`)
    })

    autoUpdater.on('update-downloaded', (info) => {
      logger.info('Updater', `Mise à jour téléchargée : ${info.version}`)
    })

    ipcMain.on('apply-update', () => {
      logger.info('Updater', 'Installation de la mise à jour...')
      autoUpdater.quitAndInstall()
    })

    autoUpdater.checkForUpdates()
  }

  ipcMain.handle('switcher:open', () => {
    logger.info('Main', 'Ouverture de la fenêtre Switcher')
    createSwitcherWindow()
  })
}

app.commandLine.appendSwitch('disable-features', 'ServiceWorker')

app.whenReady().then(() => {
  logger.info('Main', 'App prête — enregistrement des IPC')

  // Riot auth endpoint ne renvoie pas de headers CORS — on les injecte côté Electron
  session.defaultSession.webRequest.onHeadersReceived(
    { urls: ['https://auth.riotgames.com/*'] },
    (details, callback) => {
      callback({
        responseHeaders: {
          ...details.responseHeaders,
          'Access-Control-Allow-Origin': ['*'],
          'Access-Control-Allow-Headers': ['Content-Type'],
        },
      })
    }
  )
  const { registerSwitcherIpc, switcherService } = require('./ipc/switcher.ipc.cjs')
  registerLogsIpc()
  registerSwitcherIpc()
  registerSnifferIpc()
  registerProxyIpc()
  registerAutofocusIpc(switcherService)
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})
