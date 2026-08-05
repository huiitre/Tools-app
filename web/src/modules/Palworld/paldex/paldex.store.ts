import { defineStore } from 'pinia'
import { fetchPals } from './fetch/paldex.fetch'
import type { PalworldPalListItem } from './types/paldex.types'

function isIncompletePal(pal: PalworldPalListItem) {
  return (!pal.name || pal.name === 'en_text')
    && !pal.imageUrl
    && pal.paldexIndex === null
}

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
        this.pals = (await fetchPals()).filter(pal => !isIncompletePal(pal))
      } catch {
        this.error = 'Impossible de charger le Paldex.'
      } finally {
        this.loading = false
      }
    },
  },
})
