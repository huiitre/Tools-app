import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ValorantStoreHistoryView } from '@/modules/Riot/valorant/valorant.types'

export type RiotRegion = 'eu' | 'na' | 'ap' | 'kr' | 'br' | 'latam'

export interface UserSkinData {
  skinId: number
  addedAt: string
}

export interface ValorantAccount {
  id: number
  puuid: string
  region: RiotRegion
  gameName: string | null
  tagLine: string | null
  label: string | null
}

export const useRiotStore = defineStore('riot', () => {
  // Uniquement en mémoire vive
  const region = ref<RiotRegion>('eu')

  // Comptes Valorant liés
  const accounts = ref<ValorantAccount[]>([])
  const selectedAccountId = ref<number | null>(null)

  // User Skins & Watchlist state (scoped au compte sélectionné)
  const ownedSkins = ref<UserSkinData[]>([])
  const watchedSkins = ref<UserSkinData[]>([])
  const storeHistory = ref<ValorantStoreHistoryView[]>([])

  function setRegion(reg: RiotRegion) {
    region.value = reg
  }

  function clearAll() {
    accounts.value = []
    selectedAccountId.value = null
    ownedSkins.value = []
    watchedSkins.value = []
    storeHistory.value = []
    // On garde la région en mémoire pour le confort
  }

  /* =========================
     COMPTES VALORANT
  ========================= */

  function setAccounts(list: ValorantAccount[]) {
    accounts.value = list
  }

  function addAccount(account: ValorantAccount) {
    const idx = accounts.value.findIndex(a => a.id === account.id)
    if (idx === -1) accounts.value.push(account)
    else accounts.value[idx] = account
  }

  function removeAccount(id: number) {
    accounts.value = accounts.value.filter(a => a.id !== id)
  }

  function setSelectedAccountId(id: number | null) {
    selectedAccountId.value = id
  }

  /**
   * Réinitialise l'état propre au compte sélectionné (mes skins, watchlist, historique)
   * sans toucher à la liste des comptes liés ni au compte sélectionné lui-même.
   */
  function clearAccountSession() {
    ownedSkins.value = []
    watchedSkins.value = []
    storeHistory.value = []
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
   * Sync store state from a list of skins (API results)
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
    region,
    accounts,
    selectedAccountId,
    ownedSkins,
    watchedSkins,
    storeHistory,
    setRegion,
    clearAll,
    setAccounts,
    addAccount,
    removeAccount,
    setSelectedAccountId,
    clearAccountSession,
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
