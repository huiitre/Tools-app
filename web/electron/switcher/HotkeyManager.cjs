const { uIOhook } = require('uiohook-napi')
const logger = require('../logger/LoggerService.cjs')

const SVC = 'HotkeyManager'

class HotkeyManager {
  constructor(onPrev, onNext) {
    this.onPrev = onPrev
    this.onNext = onNext
    this.prevKeycode = null
    this.nextKeycode = null
    this.debounce = 1
    this.lastTrigger = 0
    this.started = false
    this.captureCallback = null
  }

  configure({ prevKeycode, nextKeycode, debounce }) {
    this.prevKeycode = prevKeycode ?? null
    this.nextKeycode = nextKeycode ?? null
    this.debounce = debounce ?? 1
    logger.info(SVC, `Configuration : prev=${this.prevKeycode} next=${this.nextKeycode} debounce=${this.debounce}ms`)
  }

  captureNextKey(callback) {
    logger.debug(SVC, 'Attente de la prochaine touche...')
    this.captureCallback = callback
  }

  start() {
    if (this.started) {
      logger.warn(SVC, 'Déjà démarré — ignoré')
      return
    }

    uIOhook.on('keyup', (e) => {
      if (this.captureCallback) {
        logger.info(SVC, `Touche capturée : keycode=${e.keycode}`)
        this.captureCallback(e.keycode)
        this.captureCallback = null
        return
      }

      const now = Date.now()
      const delta = now - this.lastTrigger
      if (delta < this.debounce) return
      this.lastTrigger = now

      if (this.prevKeycode && e.keycode === this.prevKeycode) {
        logger.debug(SVC, `Hotkey PREV déclenché (keycode=${e.keycode})`)
        this.onPrev()
      } else if (this.nextKeycode && e.keycode === this.nextKeycode) {
        logger.debug(SVC, `Hotkey NEXT déclenché (keycode=${e.keycode})`)
        this.onNext()
      }
    })

    uIOhook.start()
    this.started = true
    logger.info(SVC, 'uIOhook démarré')
  }

  stop() {
    if (!this.started) {
      logger.warn(SVC, 'Pas démarré — stop ignoré')
      return
    }
    uIOhook.stop()
    this.started = false
    logger.info(SVC, 'uIOhook arrêté')
  }
}

module.exports = { HotkeyManager }
