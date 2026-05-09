export interface ValorantSkinLevel {
  assetId: string
  levelIndex: number
  name: string | null
  displayIconUrl: string | null
  streamedVideoUrl: string | null
}

export interface ValorantSkin {
  id: number
  assetId: string
  name: string
  iconUrl: string | null
  tierUuid: string | null
  contentTierUuid: string | null
  weaponId: number | null
  levels: ValorantSkinLevel[]
  owned: boolean
  watched: boolean
  ownedAt: string | null
  watchedAt: string | null
}

export interface ValorantShopOffer extends ValorantSkin {
  cost: number
}

export interface ValorantWeapon {
  id: number
  assetId: string
  name: string
  category: string
  defaultSkinAssetId: string
  displayIconUrl: string | null
}
