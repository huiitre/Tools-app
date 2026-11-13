const fs = require('fs')
const path = require('path')
const { app } = require('electron')
const logger = require('../logger/LoggerService.cjs')

const SVC = 'SwitcherPrefs'

const DEFAULTS = {
  // Noms des personnages toujours placés en tête de liste.
  pinnedNames: [],
  // Réordonner les fenêtres selon l'ordre de passage reçu au début de chaque combat (autofocus).
  autoOrder: true,
}

// Préférences du switcher, côté process principal : elles servent même fenêtre switcher fermée.
// Un fichier plutôt que le localStorage, qui n'est pas partagé entre la fenêtre principale (app://)
// et la fenêtre switcher (file://) en production.
class SwitcherPrefs {
  constructor() {
    this.data = null
  }

  _file() {
    return path.join(app.getPath('userData'), 'switcher-prefs.json')
  }

  get() {
    if (this.data) return this.data
    try {
      this.data = { ...DEFAULTS, ...JSON.parse(fs.readFileSync(this._file(), 'utf8')) }
    } catch {
      this.data = { ...DEFAULTS }
    }
    return this.data
  }

  update(patch) {
    this.data = { ...this.get(), ...patch }
    try {
      fs.writeFileSync(this._file(), JSON.stringify(this.data, null, 2))
    } catch (e) {
      logger.error(SVC, `Écriture des préférences impossible : ${e.message}`)
    }
    return this.data
  }
}

module.exports = { SwitcherPrefs }
