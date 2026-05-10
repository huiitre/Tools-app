import type { RiotRegion } from '@/modules/Riot/riot.store'
import { clientV3 } from '@/services/axiosInstance'
import type { ValorantSkin, ValorantWeapon } from '../valorant.types'

const VP_CURRENCY_ID = '85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741'
const SKIN_TYPE_ID = 'e7c63390-eda7-46e0-bb7a-a6abdacd2433'
const CLIENT_PLATFORM =
  'ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9'

export function extractPuuid(accessToken: string): string {
  try {
    const payload = JSON.parse(atob(accessToken.split('.')[1]))
    if (!payload.sub) throw new Error()
    return payload.sub
  } catch {
    throw new Error('Token invalide — impossible de lire le PUUID')
  }
}

export async function fetchEntitlementToken(accessToken: string): Promise<string> {
  const res = await fetch('https://entitlements.auth.riotgames.com/api/token/v1', {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: '{}',
  })
  if (!res.ok) throw new Error(`Token Riot invalide ou expiré (${res.status})`)
  const data = await res.json()
  return data.entitlements_token
}

export async function fetchClientVersion(): Promise<string> {
  const { data } = await clientV3.get('/riot/valorant/version')
  return data.riotClientVersion
}

export interface RawBundle {
  dataAssetId: string
  items: Array<{ itemId: string; cost: number }>
  totalBaseCost: number
  totalDiscountedCost: number
  discountPercent: number
  remainingSeconds: number
}

export interface RawNightMarketOffer {
  BonusOfferID: string
  Offer: {
    OfferID: string
    Cost: Record<string, number>
    Rewards: Array<{ ItemID: string }>
  }
  DiscountCosts: Record<string, number>
  DiscountPercent: number
  IsSeen: boolean
}

export interface StorefrontResult {
  offers: Array<{ id: string; cost: number }>
  remainingSeconds: number
  bundles: RawBundle[]
  nightMarket: {
    offers: RawNightMarketOffer[]
    remainingSeconds: number
  } | null
}

export async function fetchStorefront(
  puuid: string,
  region: RiotRegion,
  accessToken: string,
  entitlementsToken: string,
  clientVersion: string,
): Promise<StorefrontResult> {
  const res = await fetch(`https://pd.${region}.a.pvp.net/store/v3/storefront/${puuid}`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'X-Riot-Entitlements-JWT': entitlementsToken,
      'X-Riot-ClientPlatform': CLIENT_PLATFORM,
      'X-Riot-ClientVersion': clientVersion,
    },
    body: '{}',
  })
  if (!res.ok) throw new Error(`Erreur boutique Valorant (${res.status})`)
  const data = await res.json()

  const bundles: RawBundle[] = []
  const featured = data.FeaturedBundle
  if (featured) {
    const rawBundles: any[] = featured.Bundles ?? (featured.Bundle ? [featured.Bundle] : [])
    for (const b of rawBundles) {
      const remaining = b.DurationRemainingInSeconds ?? featured.BundleRemainingDurationInSeconds ?? 0
      const items = (b.Items ?? [])
        .filter((i: any) => i.Item?.ItemTypeID === SKIN_TYPE_ID)
        .map((i: any) => ({
          itemId: i.Item.ItemID as string,
          cost: i.DiscountedPrice ?? i.BasePrice ?? 0,
        }))

      bundles.push({
        dataAssetId: b.DataAssetID,
        items,
        totalBaseCost: b.TotalBaseCost?.[VP_CURRENCY_ID] ?? 0,
        totalDiscountedCost: b.TotalDiscountedCost?.[VP_CURRENCY_ID] ?? b.TotalBaseCost?.[VP_CURRENCY_ID] ?? 0,
        discountPercent: b.TotalDiscountPercent ?? 0,
        remainingSeconds: remaining,
      })
    }
  }

  let nightMarket = null
  if (data.BonusStore) {
    nightMarket = {
      offers: data.BonusStore.BonusStoreOffers,
      remainingSeconds: data.BonusStore.BonusStoreRemainingDurationInSeconds,
    }
  }

  return {
    offers: data.SkinsPanelLayout.SingleItemStoreOffers.map((offer: any) => ({
      id: offer.Rewards[0].ItemID,
      cost: offer.Cost[VP_CURRENCY_ID] ?? 0,
    })),
    remainingSeconds: data.SkinsPanelLayout.SingleItemOffersRemainingDurationInSeconds ?? 0,
    bundles,
    nightMarket,
  }
}

export async function fetchBundleMeta(uuid: string): Promise<{ name: string; displayIcon: string }> {
  try {
    const { data } = await clientV3.get(`/riot/valorant/bundles/by-asset/${uuid}`)
    return {
      name: data.name ?? 'Pack inconnu',
      displayIcon: data.bannerUrl ?? '',
    }
  } catch {
    return { name: 'Pack inconnu', displayIcon: '' }
  }
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
  refreshToken: string,
): Promise<{ accessToken: string; refreshToken: string }> {
  const { data } = await clientV3.post('/riot/valorant/refresh-token', { refreshToken })
  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken ?? refreshToken,
  }
}

export async function fetchSkinByLevelId(levelUuid: string): Promise<ValorantSkin> {
  const { data } = await clientV3.get<ValorantSkin>(`/riot/valorant/skins/by-level/${levelUuid}`)
  return data
}

export async function fetchWeapons(): Promise<ValorantWeapon[]> {
  const { data } = await clientV3.get<ValorantWeapon[]>('/riot/valorant/weapons')
  return data
}
