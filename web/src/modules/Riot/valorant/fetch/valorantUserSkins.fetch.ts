import { clientV3 } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantStoreHistoryView } from '../valorant.types'

/* =========================
   MY SKINS (OWNED)
========================= */

export async function fetchMySkins(): Promise<ValorantSkin[]> {
  const { data } = await clientV3.get<ValorantSkin[]>('/riot/valorant/my-skins')
  return data
}

export async function addToMySkins(skinId: number): Promise<ValorantSkin> {
  const { data } = await clientV3.post<ValorantSkin>('/riot/valorant/my-skins', { skinId })
  return data
}

export async function removeFromMySkins(skinId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/my-skins/${skinId}`)
}

/* =========================
   WATCHLIST
========================= */

export async function fetchWatchlist(): Promise<ValorantSkin[]> {
  const { data } = await clientV3.get<ValorantSkin[]>('/riot/valorant/watchlist')
  return data
}

export async function addToWatchlist(skinId: number): Promise<ValorantSkin> {
  const { data } = await clientV3.post<ValorantSkin>('/riot/valorant/watchlist', { skinId })
  return data
}

export async function removeFromWatchlist(skinId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/watchlist/${skinId}`)
}

/* =========================
   STORE HISTORY
========================= */

export async function fetchStoreHistory(): Promise<ValorantStoreHistoryView[]> {
  const { data } = await clientV3.get<ValorantStoreHistoryView[]>('/riot/valorant/store-history')
  return data
}

export async function addToStoreHistory(skinIds: number[], seenAt: string): Promise<void> {
  await clientV3.post('/riot/valorant/store-history', { skinIds, seenAt })
}
