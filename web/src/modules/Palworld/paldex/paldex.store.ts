import { defineStore } from 'pinia'
import { fetchPals } from './fetch/paldex.fetch'
import type { PalworldPalListItem } from './types/paldex.types'

export const usePaldexStore = defineStore('paldex', {
  state: () => ({
    pals: [] as PalworldPalListItem[],
    loading: false,
    error: null as string | null,
  }),

  actions: {
    async ensureLoaded() {
      if (this.pals.length || this.loading) return
      this.loading = true
      this.error = null
      try {
        this.pals = await fetchPals()
      } catch {
        this.error = 'Impossible de charger le Paldex.'
      } finally {
        this.loading = false
      }
    },
  },
})
