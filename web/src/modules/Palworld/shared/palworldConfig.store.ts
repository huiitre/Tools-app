import { defineStore } from 'pinia'

export interface PalworldConfig {
  refreshIntervalSeconds: number
}

const STORAGE_KEY = 'palworld.config'
const DEFAULT_REFRESH_INTERVAL_SECONDS = 5

export const usePalworldConfigStore = defineStore('palworldConfig', {
  state: () => ({
    refreshIntervalSeconds: DEFAULT_REFRESH_INTERVAL_SECONDS,
  }),

  actions: {
    hydrate() {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) return
      try {
        const parsed = JSON.parse(stored) as Partial<PalworldConfig>
        this.refreshIntervalSeconds = this.normalize(parsed.refreshIntervalSeconds)
      } catch {
        localStorage.removeItem(STORAGE_KEY)
      }
    },

    setRefreshIntervalSeconds(value: number | null | undefined) {
      this.refreshIntervalSeconds = this.normalize(value)
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ refreshIntervalSeconds: this.refreshIntervalSeconds }))
    },

    normalize(value: number | null | undefined): number {
      if (!value || value <= 0 || Number.isNaN(value)) return DEFAULT_REFRESH_INTERVAL_SECONDS
      return Math.floor(value)
    },
  },
})
