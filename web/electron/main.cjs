const { app, BrowserWindow, ipcMain, nativeImage, session, shell, protocol, net } = require('electron')
const { autoUpdater } = require('electron-updater')
const path = require('path')
const { createSwitcherWindow } = require('./windows/dofusSwitcher.cjs')
const { registerSwitcherIpc } = require('./ipc/switcher.ipc.cjs')
const { registerSnifferIpc } = require('./ipc/sniffer.ipc.cjs')
const { registerProxyIpc } = require('./ipc/proxy.ipc.cjs')
const { registerAutofocusIpc } = require('./ipc/autofocus.ipc.cjs')
const { registerLogsIpc } = require('./ipc/logs.ipc.cjs')
const logger = require('./logger/LoggerService.cjs')

let mainWindow = null

function handleDeepLink(url) {
  try {
    const parsed = new URL(url)
    if (parsed.hostname === 'auth') {
      const token = parsed.searchParams.get('token')
      if (token && mainWindow) {
        mainWindow.webContents.send('google-auth-token', token)
        if (mainWindow.isMinimized()) mainWindow.restore()
        mainWindow.focus()
      }
    }
  } catch (e) {
    logger.error('Main', `Deep link error: ${e.message}`)
  }
}

// macOS : deep link sur instance existante
app.on('open-url', (event, url) => {
  event.preventDefault()
  handleDeepLink(url)
})

// Windows / Linux : instance unique + deep link via second-instance
const gotTheLock = app.requestSingleInstanceLock()
if (!gotTheLock) {
  app.quit()
} else {
  app.on('second-instance', (event, commandLine) => {
    const url = commandLine.find(arg => arg.startsWith('tools://'))
    if (url) handleDeepLink(url)
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore()
      mainWindow.focus()
    }
  })
}

function createWindow() {
  const appIcon = nativeImage.createFromPath(path.join(__dirname, 'icon.png'))

  mainWindow = new BrowserWindow({
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

  mainWindow.setIcon(appIcon)

  logger.setMainWindow(mainWindow)
  logger.info('Main', 'Fenêtre principale créée')

  // Initialisation du mainWindow sur les services qui en ont besoin
  const autofocusService = require('./sniffer/AutofocusService.cjs')
  const snifferService = require('./sniffer/SnifferService.cjs')
  const proxyService = require('./proxy/ProxyService.cjs')
  autofocusService.setMainWindow(mainWindow)
  snifferService.setMainWindow(mainWindow)
  proxyService.setMainWindow(mainWindow)

  const isDev = process.env.ELECTRON_DEV === 'true'

  if (isDev) {
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools()
    logger.info('Main', 'Mode DEV — chargement http://localhost:5173')
  } else {
    mainWindow.loadURL('app://./index.html')
    const devToolsFlag = process.argv.includes('--devtools')
    if (devToolsFlag) {
      mainWindow.webContents.openDevTools()
    } else {
      mainWindow.webContents.on('devtools-opened', () => {
        mainWindow.webContents.closeDevTools()
      })
    }

    autoUpdater.autoDownload = false
    autoUpdater.autoInstallOnAppQuit = false

    autoUpdater.on('checking-for-update', () => {
      logger.info('Updater', 'Vérification des mises à jour...')
    })

    autoUpdater.on('update-available', (info) => {
      logger.info('Updater', `Mise à jour disponible : ${info.version}`)
      mainWindow.webContents.send('update-available')
    })

    autoUpdater.on('update-not-available', (info) => {
      logger.info('Updater', `Pas de mise à jour. Version actuelle : ${info.version}`)
    })

    autoUpdater.on('error', (err) => {
      logger.error('Updater', `Erreur : ${err.message}`)
    })

    autoUpdater.on('download-progress', (progress) => {
      const percent = Math.round(progress.percent)
      logger.info('Updater', `Téléchargement : ${percent}%`)
      mainWindow.webContents.send('download-progress', percent)
    })

    autoUpdater.on('update-downloaded', (info) => {
      logger.info('Updater', `Mise à jour téléchargée : ${info.version}`)
      mainWindow.webContents.send('update-downloaded')
    })

    ipcMain.on('start-download', () => {
      logger.info('Updater', 'Démarrage du téléchargement...')
      autoUpdater.downloadUpdate()
    })

    ipcMain.on('apply-update', () => {
      logger.info('Updater', 'Installation de la mise à jour...')
      autoUpdater.quitAndInstall()
    })

    mainWindow.webContents.on('did-finish-load', () => {
      autoUpdater.checkForUpdates()
    })
  }

  ipcMain.handle('switcher:open', () => {
    logger.info('Main', 'Ouverture de la fenêtre Switcher')
    createSwitcherWindow()
  })
}

protocol.registerSchemesAsPrivileged([{
  scheme: 'app',
  privileges: { secure: true, standard: true, corsEnabled: true, supportFetchAPI: true }
}])

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
  app.setAsDefaultProtocolClient('tools')

  ipcMain.handle('shell:open-external', (_, url) => shell.openExternal(url))

  const { pathToFileURL } = require('url')
  protocol.handle('app', (request) => {
    const url = new URL(request.url)
    const filePath = decodeURIComponent(url.pathname)
    const relativePath = filePath === '/' ? 'index.html' : filePath.slice(1)
    const fullPath = path.normalize(path.join(__dirname, '..', 'dist', relativePath))
    return net.fetch(pathToFileURL(fullPath).toString())
  })

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
