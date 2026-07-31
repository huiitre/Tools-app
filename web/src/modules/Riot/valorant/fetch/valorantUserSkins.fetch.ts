import { clientV3 } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantStoreHistoryView } from '../valorant.types'

/* =========================
   MY SKINS (OWNED)
========================= */

export async function fetchMySkins(accountId: number): Promise<ValorantSkin[]> {
  const { data } = await clientV3.get<ValorantSkin[]>('/riot/valorant/my-skins', { params: { accountId } })
  return data
}

export async function addToMySkins(skinId: number, accountId: number): Promise<ValorantSkin> {
  const { data } = await clientV3.post<ValorantSkin>('/riot/valorant/my-skins', { skinId, accountId })
  return data
}

export async function removeFromMySkins(skinId: number, accountId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/my-skins/${skinId}`, { params: { accountId } })
}

/* =========================
   WATCHLIST
========================= */

export async function fetchWatchlist(accountId: number): Promise<ValorantSkin[]> {
  const { data } = await clientV3.get<ValorantSkin[]>('/riot/valorant/watchlist', { params: { accountId } })
  return data
}

export async function addToWatchlist(skinId: number, accountId: number): Promise<ValorantSkin> {
  const { data } = await clientV3.post<ValorantSkin>('/riot/valorant/watchlist', { skinId, accountId })
  return data
}

export async function removeFromWatchlist(skinId: number, accountId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/watchlist/${skinId}`, { params: { accountId } })
}

/* =========================
   STORE HISTORY
========================= */

export async function fetchStoreHistory(accountId: number): Promise<ValorantStoreHistoryView[]> {
  const { data } = await clientV3.get<ValorantStoreHistoryView[]>('/riot/valorant/store-history', { params: { accountId } })
  return data
}

export async function addToStoreHistory(skinIds: number[], seenAt: string, accountId: number): Promise<void> {
  await clientV3.post('/riot/valorant/store-history', { skinIds, seenAt, accountId })
}
