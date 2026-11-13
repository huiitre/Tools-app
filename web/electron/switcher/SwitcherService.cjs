const { execSync } = require('child_process')
const logger = require('../logger/LoggerService.cjs')

const SVC = 'SwitcherService'

// Suivi automatique des fenêtres : fréquence du scan léger (sans lecture mémoire du jeu).
const WATCH_INTERVAL_MS = 2000

class SwitcherService {
  // prefs : { get(), update(patch) } — voir SwitcherPrefs. En mémoire par défaut (tests).
  constructor(scanner, focuser, nameResolver, prefs = memoryPrefs()) {
    this.scanner = scanner
    this.focuser = focuser
    this.nameResolver = nameResolver
    this.prefs = prefs
    this.windows = []
    this.currentIndex = 0
    // pid du client Dofus → dernier nom de personnage détecté à sa connexion (capture réseau)
    this.detectedNames = new Map()
    this.watchTimer = null
    this.onChange = null
  }

  // Appelé à chaque changement de la liste hors d'une action de la fenêtre switcher (suivi
  // automatique, personnage détecté), pour qu'elle se mette à jour sans refresh.
  setOnChange(callback) {
    this.onChange = callback
  }

  startWatching() {
    if (this.watchTimer) return
    logger.info(SVC, 'Suivi automatique des fenêtres démarré')
    this.watchTimer = setInterval(() => this._watchTick(), WATCH_INTERVAL_MS)
    this._watchTick()
  }

  stopWatching() {
    if (!this.watchTimer) return
    clearInterval(this.watchTimer)
    this.watchTimer = null
    logger.info(SVC, 'Suivi automatique des fenêtres arrêté')
  }

  async _watchTick() {
    const before = this._signature()
    await this._lightScan()
    if (this._signature() !== before) this._emitChange()
  }

  // Scan sans résolution du nom par la mémoire du jeu : titre actuel des fenêtres, et nom
  // détecté s'il y en a un pour ce process.
  async _lightScan() {
    const scanned = await this.scanner.scan()
    const resolved = scanned.map((w) => {
      const detected = this.detectedNames.get(w.pid)
      if (detected && !this.windows.some((prev) => prev.windowId === w.windowId)) {
        this._setTitle(w.windowId, detected)
        return { ...w, characterName: detected }
      }
      return { ...w, characterName: this._stripDofusPrefix(w.characterName) }
    })
    this._merge(resolved)

    const alive = new Set(scanned.map((w) => w.pid))
    for (const pid of this.detectedNames.keys()) {
      if (!alive.has(pid)) this.detectedNames.delete(pid)
    }
  }

  // Personnage détecté à la connexion d'un client : la fenêtre prend son nom, même renommée à la
  // main avant (une nouvelle connexion peut être un autre personnage).
  async applyDetectedName(pid, name) {
    this.detectedNames.set(pid, name)
    if (!this.windows.some((w) => w.pid === pid)) await this._lightScan()

    let changed = false
    this.windows = this.windows.map((w) => {
      if (w.pid !== pid) return w
      this._setTitle(w.windowId, name)
      changed = true
      return { ...w, characterName: name }
    })
    this._applyPinned()
    logger.info(SVC, `Nom détecté pour le pid ${pid} : "${name}"`, { appliqué: changed })
    if (changed) this._emitChange()
  }

  // Bouton refresh d'une fenêtre : retirée si elle n'existe plus, sinon elle reprend le dernier
  // nom détecté pour son process, ou son titre actuel à défaut.
  async refreshWindow(windowId) {
    const scanned = await this.scanner.scan()
    const live = scanned.find((w) => w.windowId === windowId)

    if (!live) {
      this.windows = this.windows.filter((w) => w.windowId !== windowId)
      logger.info(SVC, `Refresh wid=${windowId} : fenêtre fermée, retirée`)
      return this.windows
    }

    const detected = this.detectedNames.get(live.pid)
    if (detected) this._setTitle(windowId, detected)
    const characterName = detected ?? this._stripDofusPrefix(live.characterName)

    this.windows = this.windows.map((w) =>
      w.windowId === windowId ? { ...w, pid: live.pid, characterName } : w
    )
    this._applyPinned()
    logger.info(SVC, `Refresh wid=${windowId} → "${characterName}"`, { détecté: !!detected })
    return this.windows
  }

  _setTitle(windowId, name) {
    try {
      execSync(`xdotool set_window --name "${name}" ${windowId}`)
    } catch {}
  }

  _signature() {
    return this.windows.map((w) => `${w.windowId}:${w.characterName}:${w.pinned ? 1 : 0}`).join('|')
  }

  _emitChange() {
    if (this.onChange) this.onChange(this.windows)
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
    this._applyPinned()
    logger.debug(SVC, 'Ordre des fenêtres mis à jour', windowIds)
    return this.windows
  }

  renameWindow(windowId, characterName) {
    const previous = this.windows.find((w) => w.windowId === windowId)
    // L'épinglage suit le personnage : renommer une fenêtre épinglée déplace l'épingle.
    if (previous?.pinned && previous.characterName !== characterName) {
      const pinned = this.prefs.get().pinnedNames.filter((n) => n !== previous.characterName)
      this.prefs.update({ pinnedNames: [...pinned, characterName] })
    }
    this.windows = this.windows.map((w) =>
      w.windowId === windowId ? { ...w, characterName } : w
    )
    this._applyPinned()
    logger.info(SVC, `Renommage wid=${windowId} → "${characterName}"`)
    return this.windows
  }

  // Épinglage par nom de personnage : il survit à la relance du jeu, qui change les fenêtres.
  setPinned(windowId, pinned) {
    const target = this.windows.find((w) => w.windowId === windowId)
    if (!target?.characterName) return this.windows
    const names = this.prefs.get().pinnedNames.filter((n) => n !== target.characterName)
    this.prefs.update({ pinnedNames: pinned ? [...names, target.characterName] : names })
    this._applyPinned()
    logger.info(SVC, `${pinned ? 'Épinglage' : 'Désépinglage'} de "${target.characterName}"`)
    return this.windows
  }

  getPrefs() {
    return this.prefs.get()
  }

  setAutoOrder(enabled) {
    this.prefs.update({ autoOrder: !!enabled })
    logger.info(SVC, `Réordonnancement selon l'ordre de combat : ${enabled ? 'activé' : 'désactivé'}`)
    return this.prefs.get()
  }

  // Ordre de passage reçu au début d'un combat (noms des personnages, autofocus) : épinglées en
  // tête, puis les fenêtres du combat dans cet ordre, puis les autres dans leur ordre actuel.
  applyFightOrder(names) {
    if (!this.prefs.get().autoOrder) return
    const rank = new Map(names.map((name, i) => [name, i]))
    const inFight = (w) => !w.pinned && rank.has(w.characterName)

    const before = this._signature()
    this.windows = [
      ...this.windows.filter((w) => w.pinned),
      ...this.windows.filter(inFight).sort((a, b) => rank.get(a.characterName) - rank.get(b.characterName)),
      ...this.windows.filter((w) => !w.pinned && !inFight(w)),
    ]
    if (this._signature() === before) return

    this.currentIndex = 0
    logger.info(SVC, 'Fenêtres réordonnées selon l\'ordre de combat', this.windows.map((w) => w.characterName))
    this._emitChange()
  }

  // Marque les fenêtres épinglées et les remonte en tête, sans changer l'ordre au sein de chaque groupe.
  _applyPinned() {
    const pinnedNames = new Set(this.prefs.get().pinnedNames)
    const marked = this.windows.map((w) => ({ ...w, pinned: pinnedNames.has(w.characterName) }))
    this.windows = [...marked.filter((w) => w.pinned), ...marked.filter((w) => !w.pinned)]
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

  // L'ordre choisi par glisser-déposer est conservé : les fenêtres connues gardent leur place,
  // les nouvelles s'ajoutent à la fin, les fermées disparaissent. Reconstruire la liste dans
  // l'ordre de `xdotool search` écrasait cet ordre à chaque scan.
  _merge(scanned) {
    const scannedById = new Map(scanned.map((w) => [w.windowId, w]))

    const kept = this.windows
      .filter((prev) => scannedById.has(prev.windowId))
      .map((prev) => {
        const w = scannedById.get(prev.windowId)
        return {
          ...w,
          enabled: prev.enabled,
          characterName: w.characterName ?? prev.characterName,
        }
      })

    const known = new Set(this.windows.map((w) => w.windowId))
    const added = scanned.filter((w) => !known.has(w.windowId))

    this.windows = [...kept, ...added]
    this._applyPinned()
  }
}

function memoryPrefs() {
  let data = { pinnedNames: [], autoOrder: true }
  return { get: () => data, update: (patch) => (data = { ...data, ...patch }) }
}

module.exports = { SwitcherService }
