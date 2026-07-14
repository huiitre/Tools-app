import { defineStore } from 'pinia'
import { fetchPalworldTierLists } from './fetch/palworldTierList.fetch'
import type { PalworldTierListsByCategory } from './types/palworldTierList.types'

export const usePalworldTierListStore = defineStore('palworldTierList', {
  state: () => ({
    data: null as PalworldTierListsByCategory | null,
    loading: false,
    error: null as string | null,
  }),

  actions: {
    async ensureLoaded() {
      if (this.data || this.loading) return
      this.loading = true
      this.error = null
      try {
        this.data = await fetchPalworldTierLists()
      } catch {
        this.error = 'Impossible de charger la tier list.'
      } finally {
        this.loading = false
      }
    },
  },
})
