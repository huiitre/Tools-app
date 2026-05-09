const { execSync } = require('child_process')
const logger = require('../logger/LoggerService.cjs')

const SVC = 'SwitcherService'

class SwitcherService {
  constructor(scanner, focuser, nameResolver) {
    this.scanner = scanner
    this.focuser = focuser
    this.nameResolver = nameResolver
    this.windows = []
    this.currentIndex = 0
  }

  async scan() {
    logger.debug(SVC, 'Scan des fenêtres Dofus...')
    const scanned = await this.scanner.scan()
    const resolved = await Promise.all(
      scanned.map(async (w) => {
        const characterName = await this.nameResolver.resolve(w.pid)
        if (characterName) {
          try {
            execSync(`xdotool set_window --name "${characterName}" ${w.windowId}`)
          } catch {}
        }
        return { ...w, characterName: characterName ?? this._stripDofusPrefix(w.characterName) }
      })
    )
    this._merge(resolved)
    logger.info(SVC, `Scan terminé : ${this.windows.length} fenêtre(s)`, this.windows.map(w => w.characterName))
    return this.windows
  }

  _stripDofusPrefix(name) {
    if (!name) return null
    return name.replace(/^Dofus\s*-\s*/i, '').trim() || null
  }

  async focusNext() {
    const enabled = this._enabled()
    if (!enabled.length) {
      logger.warn(SVC, 'focusNext : aucune fenêtre active')
      return
    }
    this.currentIndex = (this.currentIndex + 1) % enabled.length
    const target = enabled[this.currentIndex]
    logger.info(SVC, `focusNext → "${target.characterName}" (wid=${target.windowId})`)
    await this.focuser.focus(target.windowId)
  }

  async focusPrev() {
    const enabled = this._enabled()
    if (!enabled.length) {
      logger.warn(SVC, 'focusPrev : aucune fenêtre active')
      return
    }
    this.currentIndex = (this.currentIndex - 1 + enabled.length) % enabled.length
    const target = enabled[this.currentIndex]
    logger.info(SVC, `focusPrev → "${target.characterName}" (wid=${target.windowId})`)
    await this.focuser.focus(target.windowId)
  }

  async focusWindow(windowId) {
    const enabled = this._enabled()
    const idx = enabled.findIndex(w => w.windowId === windowId)
    if (idx !== -1) this.currentIndex = idx
    const target = enabled[idx] ?? { windowId, characterName: '?' }
    logger.info(SVC, `focusWindow "${target.characterName}" (wid=${windowId})`)
    await this.focuser.focus(windowId)
  }

  setEnabled(windowId, enabled) {
    this.windows = this.windows.map((w) =>
      w.windowId === windowId ? { ...w, enabled } : w
    )
    logger.info(SVC, `setEnabled wid=${windowId} → ${enabled}`)
    return this.windows
  }

  setOrder(windowIds) {
    const map = new Map(this.windows.map((w) => [w.windowId, w]))
    this.windows = windowIds.map((id) => map.get(id)).filter(Boolean)
    logger.debug(SVC, 'Ordre des fenêtres mis à jour', windowIds)
    return this.windows
  }

  renameWindow(windowId, characterName) {
    this.windows = this.windows.map((w) =>
      w.windowId === windowId ? { ...w, characterName } : w
    )
    logger.info(SVC, `Renommage wid=${windowId} → "${characterName}"`)
    return this.windows
  }

  getWindows() {
    return this.windows
  }

  getCurrentWindowId() {
    const enabled = this._enabled()
    if (!enabled.length) return null
    return enabled[this.currentIndex]?.windowId ?? null
  }

  _enabled() {
    return this.windows.filter((w) => w.enabled)
  }

  _merge(scanned) {
    const existing = new Map(this.windows.map((w) => [w.windowId, w]))
    this.windows = scanned.map((w) => {
      const prev = existing.get(w.windowId)
      if (!prev) return w
      return {
        ...w,
        enabled: prev.enabled,
        characterName: w.characterName ?? prev.characterName,
      }
    })
  }
}

module.exports = { SwitcherService }
