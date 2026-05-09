const { execSync } = require('child_process')

class LinuxWindowScanner {
  async scan() {
    try {
      const out = execSync('xdotool search --class Dofus').toString().trim()
      const windowIds = out.split('\n').filter(Boolean)
      return windowIds.map((windowId) => ({
        windowId,
        pid: this._getPid(windowId),
        characterName: this._getWindowName(windowId),
        enabled: true,
      }))
    } catch {
      return []
    }
  }

  _getPid(windowId) {
    try {
      return parseInt(execSync(`xdotool getwindowpid ${windowId}`).toString().trim(), 10)
    } catch {
      return -1
    }
  }

  _getWindowName(windowId) {
    try {
      return execSync(`xdotool getwindowname ${windowId}`).toString().trim() || null
    } catch {
      return null
    }
  }
}

module.exports = { LinuxWindowScanner }