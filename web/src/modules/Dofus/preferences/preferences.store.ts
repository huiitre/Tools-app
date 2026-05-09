import { defineStore } from 'pinia'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'

/* ======================
   TYPES
====================== */

type DofusConfigState = {
  priceDisplayMode: PriceDisplayMode
  showItemContextOnHover: boolean
}

/* ======================
   CONSTANTS
====================== */

const STORAGE_KEY_PRICE_MODE = 'dofus.priceDisplayMode'
const STORAGE_KEY_SHOW_CONTEXT_ITEM = 'dofus.showItemContextOnHover'

const DEFAULT_PRICE_MODE: PriceDisplayMode = PriceDisplayMode.COMMUNITY
const DEFAULT_SHOW_ITEM_CONTEXT = true

/* ======================
   STORE
====================== */

export const useDofusConfigStore = defineStore('dofus.preferences', {
  state: (): DofusConfigState => ({
    priceDisplayMode: DEFAULT_PRICE_MODE,
    showItemContextOnHover: DEFAULT_SHOW_ITEM_CONTEXT,
  }),

  getters: {
    isUserPrice: (s) => s.priceDisplayMode === PriceDisplayMode.USER,
    isCommunityPrice: (s) =>
      s.priceDisplayMode === PriceDisplayMode.COMMUNITY,
    isLastPrice: (s) => s.priceDisplayMode === PriceDisplayMode.LAST,
  },

  actions: {
    /* ===== PRICE MODE ===== */

    setPriceDisplayMode(mode: PriceDisplayMode) {
      this.priceDisplayMode = mode
      localStorage.setItem(STORAGE_KEY_PRICE_MODE, mode)
    },

    /* ===== OTHER PRICES ON HOVER ===== */

    setShowItemContextOnHover(value: boolean) {
      this.showItemContextOnHover = value
      localStorage.setItem(
        STORAGE_KEY_SHOW_CONTEXT_ITEM,
        String(value),
      )
    },

    /* ===== HYDRATION ===== */

    hydrateFromStorage() {
      // priceDisplayMode
      const storedPriceMode = localStorage.getItem(
        STORAGE_KEY_PRICE_MODE,
      )

      if (
        storedPriceMode === PriceDisplayMode.USER ||
        storedPriceMode === PriceDisplayMode.COMMUNITY ||
        storedPriceMode === PriceDisplayMode.LAST
      ) {
        this.priceDisplayMode = storedPriceMode
      } else {
        this.priceDisplayMode = DEFAULT_PRICE_MODE
        localStorage.removeItem(STORAGE_KEY_PRICE_MODE)
      }

      // showItemContextOnHover
      const storedShowItemContext = localStorage.getItem(
        STORAGE_KEY_SHOW_CONTEXT_ITEM,
      )

      if (storedShowItemContext === 'true') {
        this.showItemContextOnHover = true
      } else if (storedShowItemContext === 'false') {
        this.showItemContextOnHover = false
      } else {
        this.showItemContextOnHover = DEFAULT_SHOW_ITEM_CONTEXT
        localStorage.removeItem(STORAGE_KEY_SHOW_CONTEXT_ITEM)
      }
    },

    /* ===== RESET ===== */

    clear() {
      this.priceDisplayMode = DEFAULT_PRICE_MODE
      this.showItemContextOnHover = DEFAULT_SHOW_ITEM_CONTEXT
      localStorage.removeItem(STORAGE_KEY_PRICE_MODE)
      localStorage.removeItem(STORAGE_KEY_SHOW_CONTEXT_ITEM)
    },
  },
})
