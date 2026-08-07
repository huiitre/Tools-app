import { defineStore } from 'pinia'
import { fetchMerchants } from './fetch/shop.fetch'
import type { Merchant } from './types/shop.types'

const FAVORITES_STORAGE_KEY = 'palworld.shop.favoriteMerchants'

export const useShopStore = defineStore('palworldShop', {
  state: () => ({
    merchants: [] as Merchant[],
    loading: false,
    error: null as string | null,
    favoriteMerchantIds: [] as string[],
  }),

  getters: {
    merchantsSellingItem: (state) => (itemSlug: string) =>
      state.merchants
        .map(merchant => ({ merchant, offer: merchant.offers.find(o => o.itemSlug === itemSlug) }))
        .filter((entry): entry is { merchant: Merchant, offer: NonNullable<typeof entry.offer> } => !!entry.offer)
        .sort((a, b) => a.offer.price - b.offer.price),

    isFavorite: state => (externalId: string) => state.favoriteMerchantIds.includes(externalId),
  },

  actions: {
    async ensureLoaded() {
      if (this.merchants.length || this.loading) return
      this.loading = true
      this.error = null
      try {
        this.merchants = await fetchMerchants()
      } catch {
        this.error = 'Impossible de charger les marchands.'
      } finally {
        this.loading = false
      }
    },

    hydrateFavorites() {
      const stored = localStorage.getItem(FAVORITES_STORAGE_KEY)
      if (!stored) return
      try {
        const parsed = JSON.parse(stored)
        this.favoriteMerchantIds = Array.isArray(parsed) ? parsed.filter(id => typeof id === 'string') : []
      } catch {
        localStorage.removeItem(FAVORITES_STORAGE_KEY)
      }
    },

    toggleFavorite(externalId: string) {
      this.favoriteMerchantIds = this.isFavorite(externalId)
        ? this.favoriteMerchantIds.filter(id => id !== externalId)
        : [...this.favoriteMerchantIds, externalId]
      localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify(this.favoriteMerchantIds))
    },
  },
})
