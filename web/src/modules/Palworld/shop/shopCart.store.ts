import { defineStore } from 'pinia'
import type { ShopCartLine, ShopCartState } from './shopCart.types'

const STORAGE_KEY = 'palworld.shopCart'

function findLine(lines: ShopCartLine[], merchantId: string, itemSlug: string) {
  return lines.find(l => l.merchantId === merchantId && l.itemSlug === itemSlug)
}

export const useShopCartStore = defineStore('palworldShopCart', {
  state: (): ShopCartState => ({
    current: [],
    presets: {},
  }),

  getters: {
    presetNames: state => Object.keys(state.presets).sort((a, b) => a.localeCompare(b)),
  },

  actions: {
    hydrate() {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) return
      try {
        const parsed = JSON.parse(stored) as Partial<ShopCartState>
        this.current = Array.isArray(parsed.current) ? parsed.current : []
        this.presets = parsed.presets && typeof parsed.presets === 'object' ? parsed.presets : {}
      } catch {
        localStorage.removeItem(STORAGE_KEY)
      }
    },

    persist() {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ current: this.current, presets: this.presets }))
    },

    addToCart(merchantId: string, itemSlug: string, quantity: number) {
      if (quantity <= 0) return
      const existing = findLine(this.current, merchantId, itemSlug)
      if (existing) existing.quantity += quantity
      else this.current.push({ merchantId, itemSlug, quantity })
      this.persist()
    },

    setQuantity(merchantId: string, itemSlug: string, quantity: number) {
      if (quantity <= 0) {
        this.removeLine(merchantId, itemSlug)
        return
      }
      const existing = findLine(this.current, merchantId, itemSlug)
      if (existing) existing.quantity = quantity
      this.persist()
    },

    removeLine(merchantId: string, itemSlug: string) {
      this.current = this.current.filter(l => !(l.merchantId === merchantId && l.itemSlug === itemSlug))
      this.persist()
    },

    clearCurrent() {
      this.current = []
      this.persist()
    },

    savePreset(name: string) {
      const trimmed = name.trim()
      if (!trimmed) return
      this.presets[trimmed] = this.current.map(l => ({ ...l }))
      this.persist()
    },

    loadPreset(name: string) {
      const preset = this.presets[name]
      if (!preset) return
      this.current = preset.map(l => ({ ...l }))
      this.persist()
    },

    deletePreset(name: string) {
      delete this.presets[name]
      this.persist()
    },
  },
})
