import { defineStore } from 'pinia'
import LS from '@/services/localStorage'
import { useAutofocusMapping } from '@/modules/Dofus/switcher/composables/useAutofocusMapping'
import type { CaptureStatus } from './capture.types'

//* Capture réseau Dofus 3 : un seul tshark côté Electron, allumé dès que l'autofocus ou le
//* sniffer HDV est activé. Elle tourne en tâche de fond, indépendamment de la fenêtre switcher
//* et de la page HDV : les prix moyens n'arrivent qu'à la connexion d'un personnage.

const LS_KEY = 'dofus.capture'
const LEGACY_SWITCHER_KEY = 'switcher:hotkeys'

type CapturePrefs = {
  autofocus: boolean
  hdvSniffer: boolean
}

type CaptureFeature = keyof CapturePrefs

//* Les listeners IPC ne se retirent pas : un seul enregistrement pour toute la vie de la page,
//* même après une déconnexion puis une reconnexion du compte Tools.
let statusListenerRegistered = false

const loadPrefs = (): CapturePrefs => {
  const saved = LS.get(LS_KEY)
  if (saved && typeof saved === 'object') {
    return { autofocus: !!saved.autofocus, hdvSniffer: !!saved.hdvSniffer }
  }

  //* L'autofocus s'activait auparavant depuis les paramètres du switcher.
  const legacy = LS.get(LEGACY_SWITCHER_KEY)
  return { autofocus: !!legacy?.autofocus, hdvSniffer: false }
}

export const useCaptureStore = defineStore('dofusCapture', {
  state: () => ({
    prefs: { autofocus: false, hdvSniffer: false } as CapturePrefs,
    status: null as CaptureStatus | null,
    errors: { autofocus: null, hdvSniffer: null } as Record<CaptureFeature, string | null>,
    initialized: false,
  }),

  getters: {
    isRunning: (s) => !!s.status?.running,
    isFeatureActive: (s) => (feature: CaptureFeature) => {
      const consumer = feature === 'autofocus' ? 'autofocus' : 'prices'
      return !!s.status?.consumers.includes(consumer)
    },
    tsharkMissing: (s) => s.status?.requirements?.tsharkInstalled === false,
    permissionsMissing: (s) =>
      s.status?.requirements?.tsharkInstalled === true && s.status.requirements.tsharkPermissions === false,
  },

  actions: {
    async init() {
      if (this.initialized || !window.electron) return
      this.initialized = true
      this.prefs = loadPrefs()

      if (!statusListenerRegistered) {
        statusListenerRegistered = true
        window.electron.onCaptureStatus((status) => {
          useCaptureStore().status = status
        })
      }

      await window.electron.checkCaptureRequirements()
      this.status = await window.electron.getCaptureStatus()

      await Promise.all([this.applyAutofocus(), this.applyHdvSniffer()])
    },

    async shutdown() {
      if (!this.initialized) return
      this.initialized = false
      await window.electron?.stopAllCaptures()
    },

    async setFeature(feature: CaptureFeature, enabled: boolean) {
      this.prefs[feature] = enabled
      LS.set(LS_KEY, this.prefs)

      if (feature === 'autofocus') await this.applyAutofocus()
      else await this.applyHdvSniffer()
    },

    async applyAutofocus() {
      if (!window.electron) return
      this.errors.autofocus = null

      if (!this.prefs.autofocus) {
        await window.electron.stopAutofocus()
        return
      }

      const { mapping, loadMapping } = useAutofocusMapping()
      await loadMapping()
      const result = await window.electron.startAutofocus({ mapping: { ...mapping.value } })
      if (!result.success) this.errors.autofocus = result.error ?? 'Démarrage impossible'
    },

    async applyHdvSniffer() {
      if (!window.electron) return
      this.errors.hdvSniffer = null

      if (!this.prefs.hdvSniffer) {
        await window.electron.stopPriceCapture()
        return
      }

      const result = await window.electron.startPriceCapture()
      if (!result.success) this.errors.hdvSniffer = result.error ?? 'Démarrage impossible'
    },

    async recheckRequirements() {
      await window.electron?.checkCaptureRequirements()
    },
  },
})
