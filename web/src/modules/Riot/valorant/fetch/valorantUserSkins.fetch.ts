import { clientV3 } from '@/services/axiosInstance'

export interface UserSkinLink {
  id: number
  userId: number
  skinId: number
  createdAt: string
}

/* =========================
   MY SKINS (OWNED)
========================= */

export async function fetchMySkins(): Promise<UserSkinLink[]> {
  const { data } = await clientV3.get<UserSkinLink[]>('/riot/valorant/my-skins')
  return data
}

export async function addToMySkins(skinId: number): Promise<void> {
  await clientV3.post('/riot/valorant/my-skins', { skinId })
}

export async function removeFromMySkins(skinId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/my-skins/${skinId}`)
}

/* =========================
   WATCHLIST
========================= */

export async function fetchWatchlist(): Promise<UserSkinLink[]> {
  const { data } = await clientV3.get<UserSkinLink[]>('/riot/valorant/watchlist')
  return data
}

export async function addToWatchlist(skinId: number): Promise<void> {
  await clientV3.post('/riot/valorant/watchlist', { skinId })
}

export async function removeFromWatchlist(skinId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/watchlist/${skinId}`)
}
