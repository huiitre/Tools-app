import { clientCore } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantStoreHistoryView } from '../valorant.types'

/* =========================
   MY SKINS (OWNED)
========================= */

export async function fetchMySkins(accountId: number): Promise<ValorantSkin[]> {
  const { data } = await clientCore.get<ValorantSkin[]>('/riot/valorant/my-skins', { params: { accountId } })
  return data
}

export async function addToMySkins(skinId: number, accountId: number): Promise<ValorantSkin> {
  const { data } = await clientCore.post<ValorantSkin>('/riot/valorant/my-skins', { skinId, accountId })
  return data
}

export async function removeFromMySkins(skinId: number, accountId: number): Promise<void> {
  await clientCore.delete(`/riot/valorant/my-skins/${skinId}`, { params: { accountId } })
}

/* =========================
   WATCHLIST
========================= */

export async function fetchWatchlist(accountId: number): Promise<ValorantSkin[]> {
  const { data } = await clientCore.get<ValorantSkin[]>('/riot/valorant/watchlist', { params: { accountId } })
  return data
}

export async function addToWatchlist(skinId: number, accountId: number): Promise<ValorantSkin> {
  const { data } = await clientCore.post<ValorantSkin>('/riot/valorant/watchlist', { skinId, accountId })
  return data
}

export async function removeFromWatchlist(skinId: number, accountId: number): Promise<void> {
  await clientCore.delete(`/riot/valorant/watchlist/${skinId}`, { params: { accountId } })
}

/* =========================
   STORE HISTORY
========================= */

export async function fetchStoreHistory(accountId: number): Promise<ValorantStoreHistoryView[]> {
  const { data } = await clientCore.get<ValorantStoreHistoryView[]>('/riot/valorant/store-history', { params: { accountId } })
  return data
}

export async function addToStoreHistory(skinIds: number[], seenAt: string, accountId: number): Promise<void> {
  await clientCore.post('/riot/valorant/store-history', { skinIds, seenAt, accountId })
}
