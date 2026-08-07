import { defineStore } from 'pinia'
import { fetchItems } from './fetch/items.fetch'
import type { ItemCatalogEntry } from './types/items.types'

export const useItemsStore = defineStore('palworldItems', {
  state: () => ({
    items: [] as ItemCatalogEntry[],
    loading: false,
    error: null as string | null,
  }),

  actions: {
    async ensureLoaded() {
      if (this.items.length || this.loading) return
      this.loading = true
      this.error = null
      try {
        this.items = await fetchItems()
      } catch {
        this.error = 'Impossible de charger les objets.'
      } finally {
        this.loading = false
      }
    },
  },
})
