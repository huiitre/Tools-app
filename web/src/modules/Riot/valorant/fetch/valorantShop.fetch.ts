import { clientV3 } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantWeapon, ValorantStoreView } from '../valorant.types'

export async function fetchStore(accessToken?: string, region?: string): Promise<ValorantStoreView> {
  const headers: Record<string, string> = {}
  if (accessToken) headers['X-Riot-Token'] = accessToken
  
  const params: Record<string, string> = {}
  if (region) params.region = region
  
  const { data } = await clientV3.get<ValorantStoreView>('/riot/valorant/store', { headers, params })
  return data
}

export async function fetchClientVersion(): Promise<string> {
  const { data } = await clientV3.get('/riot/valorant/version')
  return data.riotClientVersion
}

export function isAccessTokenExpired(accessToken: string): boolean {
  try {
    const payload = JSON.parse(atob(accessToken.split('.')[1]))
    return payload.exp * 1_000 < Date.now()
  } catch {
    return true
  }
}

export async function refreshToAccessToken(
  refreshToken?: string,
): Promise<{ accessToken: string }> {
  // Si on a un refresh token, on l'utilise pour la liaison initiale
  if (refreshToken) {
    const { data } = await clientV3.post('/riot/valorant/refresh-token', { refreshToken, region: 'eu' }) // region hardcodée par défaut si non fournie
    return { accessToken: data.accessToken }
  }
  
  // Sinon on utilise la session persistante serveur
  const { data } = await clientV3.get('/riot/valorant/refresh')
  return { accessToken: data.accessToken }
}

export async function fetchSkinByLevelId(levelUuid: string): Promise<ValorantSkin> {
  const { data } = await clientV3.get<ValorantSkin>(`/riot/valorant/skins/by-level/${levelUuid}`)
  return data
}

export async function fetchWeapons(): Promise<ValorantWeapon[]> {
  const { data } = await clientV3.get<ValorantWeapon[]>('/riot/valorant/weapons')
  return data
}
