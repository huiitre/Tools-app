import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ValorantStoreHistoryView } from '@/modules/Riot/valorant/valorant.types'

const STORAGE_KEY = 'riot.auth'

export type RiotRegion = 'eu' | 'na' | 'ap' | 'kr' | 'br' | 'latam'

interface RiotAuth {
  accessToken: string | null
  refreshToken: string | null
  region: RiotRegion
}

export interface UserSkinData {
  skinId: number
  addedAt: string
}

export const useRiotStore = defineStore('riot', () => {
  const stored = (() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return null
      return JSON.parse(raw) as RiotAuth
    } catch {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
  })()

  const accessToken = ref<string | null>(stored?.accessToken ?? null)
  const refreshToken = ref<string | null>(stored?.refreshToken ?? null)
  const region = ref<RiotRegion>(stored?.region ?? 'eu')

  // User Skins & Watchlist state
  const ownedSkins = ref<UserSkinData[]>([])
  const watchedSkins = ref<UserSkinData[]>([])
  const storeHistory = ref<ValorantStoreHistoryView[]>([])

  function setAuth(token: string, reg: RiotRegion) {
    accessToken.value = token
    region.value = reg
    persist()
  }

  function setAccessToken(token: string) {
    accessToken.value = token
    persist()
  }

  function setRefreshToken(token: string) {
    refreshToken.value = token
    persist()
  }

  function setRegion(reg: RiotRegion) {
    region.value = reg
    persist()
  }

  function clearAll() {
    accessToken.value = null
    refreshToken.value = null
    localStorage.removeItem(STORAGE_KEY)
  }

  function persist() {
    const data: RiotAuth = {
      accessToken: accessToken.value,
      refreshToken: refreshToken.value,
      region: region.value,
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  }

  /* =========================
     MY SKINS (OWNED)
  ========================= */

  function setOwnedSkins(skins: UserSkinData[]) {
    ownedSkins.value = skins
  }

  function isSkinOwned(id: number) {
    return ownedSkins.value.some(s => s.skinId === id)
  }

  function getOwnedAddedAt(id: number) {
    return ownedSkins.value.find(s => s.skinId === id)?.addedAt ?? null
  }

  function toggleOwnedLocally(id: number, active: boolean) {
    if (active) {
      if (!isSkinOwned(id)) {
        ownedSkins.value.push({ skinId: id, addedAt: new Date().toISOString() })
      }
      removeFromWatchlistLocally(id)
    } else {
      ownedSkins.value = ownedSkins.value.filter(s => s.skinId !== id)
    }
  }

  /* =========================
     WATCHLIST
  ========================= */

  function setWatchedSkins(skins: UserSkinData[]) {
    watchedSkins.value = skins
  }

  function isSkinWatched(id: number) {
    return watchedSkins.value.some(s => s.skinId === id)
  }

  function getWatchedAddedAt(id: number) {
    return watchedSkins.value.find(s => s.skinId === id)?.addedAt ?? null
  }

  function addToWatchlistLocally(id: number) {
    if (!isSkinWatched(id)) {
      watchedSkins.value.push({ skinId: id, addedAt: new Date().toISOString() })
    }
  }

  function removeFromWatchlistLocally(id: number) {
    watchedSkins.value = watchedSkins.value.filter(ws => ws.skinId !== id)
  }

  /* =========================
     STORE HISTORY
  ========================= */

  function setStoreHistory(history: ValorantStoreHistoryView[]) {
    storeHistory.value = history
  }

  /**
   * New: Sync store state from a list of skins (API results)
   */
  function syncFromSkins(skins: { id: number, owned: boolean, watched: boolean, ownedAt?: string | null, watchedAt?: string | null }[]) {
    skins.forEach(s => {
      // Owned
      if (s.owned && !isSkinOwned(s.id)) {
        ownedSkins.value.push({ skinId: s.id, addedAt: s.ownedAt ?? new Date().toISOString() })
      } else if (!s.owned && isSkinOwned(s.id)) {
        ownedSkins.value = ownedSkins.value.filter(os => os.skinId !== s.id)
      }

      // Watched
      if (s.watched && !isSkinWatched(s.id)) {
        watchedSkins.value.push({ skinId: s.id, addedAt: s.watchedAt ?? new Date().toISOString() })
      } else if (!s.watched && isSkinWatched(s.id)) {
        watchedSkins.value = watchedSkins.value.filter(ws => ws.skinId !== s.id)
      }
    })
  }

  return {
    accessToken,
    refreshToken,
    region,
    ownedSkins,
    watchedSkins,
    storeHistory,
    setAuth,
    setAccessToken,
    setRefreshToken,
    setRegion,
    clearAll,
    setOwnedSkins,
    isSkinOwned,
    getOwnedAddedAt,
    toggleOwnedLocally,
    setWatchedSkins,
    isSkinWatched,
    getWatchedAddedAt,
    addToWatchlistLocally,
    removeFromWatchlistLocally,
    setStoreHistory,
    syncFromSkins
  }
})
