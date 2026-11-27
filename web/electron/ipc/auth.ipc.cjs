const { ipcMain } = require('electron')
const googleLoopbackServer = require('../auth/GoogleLoopbackServer.cjs')

// getMainWindow : la fenêtre peut être recréée, elle est relue à la réception du token.
function registerAuthIpc(getMainWindow) {
  ipcMain.handle('auth:google-loopback-start', () =>
    googleLoopbackServer.start((token) => {
      const win = getMainWindow()
      if (!win) return
      win.webContents.send('google-auth-token', token)
      if (win.isMinimized()) win.restore()
      win.focus()
    })
  )
}

module.exports = { registerAuthIpc }
