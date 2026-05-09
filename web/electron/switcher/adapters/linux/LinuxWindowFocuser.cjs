const { execSync } = require('child_process')

class LinuxWindowFocuser {
  async focus(windowId) {
    try {
      execSync(`xdotool windowactivate ${windowId}`)
    } catch {}
  }
}

module.exports = { LinuxWindowFocuser }