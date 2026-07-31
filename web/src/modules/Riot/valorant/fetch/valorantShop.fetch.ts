import { clientV3 } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantWeapon, ValorantStoreView } from '../valorant.types'

export async function fetchStore(accountId: number, region?: string): Promise<ValorantStoreView> {
  const params: Record<string, string | number> = { accountId }
  if (region) params.region = region

  const { data } = await clientV3.get<ValorantStoreView>('/riot/valorant/store', { params })
  return data
}

export async function fetchClientVersion(): Promise<string> {
  const { data } = await clientV3.get('/riot/valorant/version')
  return data.riotClientVersion
}

export async function fetchSkinByLevelId(levelUuid: string): Promise<ValorantSkin> {
  const { data } = await clientV3.get<ValorantSkin>(`/riot/valorant/skins/by-level/${levelUuid}`)
  return data
}

export async function fetchWeapons(): Promise<ValorantWeapon[]> {
  const { data } = await clientV3.get<ValorantWeapon[]>('/riot/valorant/weapons')
  return data
}
