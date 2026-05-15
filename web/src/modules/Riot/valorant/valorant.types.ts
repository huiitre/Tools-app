export interface ValorantSkinLevel {
  assetId: string
  levelIndex: number
  name: string | null
  levelItem: string | null
  displayIconUrl: string | null
  streamedVideoUrl: string | null
}

export interface ValorantSkinChroma {
  assetId: string
  chromaIndex: number
  name: string | null
  displayIconUrl: string | null
  fullRenderUrl: string | null
  swatchUrl: string | null
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
  chromas: ValorantSkinChroma[]
  owned: boolean
  watched: boolean
  ownedAt: string | null
  watchedAt: string | null
}

export interface ValorantStoreOffer {
  skin: ValorantSkin
  cost: number
}

export interface ValorantStoreBundle {
  assetId: string
  name: string
  bannerUrl: string
  items: ValorantStoreOffer[]
  totalBaseCost: number
  totalDiscountedCost: number
  discountPercent: number
  remainingSeconds: number
}

export interface ValorantNightMarketOffer {
  offerId: string
  skin: ValorantSkin
  originalCost: number
  discountedCost: number
  discountPercent: number
  isSeen: boolean
}

export interface ValorantNightMarket {
  offers: ValorantNightMarketOffer[]
  remainingSeconds: number
}

export interface ValorantStoreView {
  offers: ValorantStoreOffer[]
  remainingSeconds: number
  bundles: ValorantStoreBundle[]
  nightMarket: ValorantNightMarket | null
}

export interface ValorantStoreHistoryView {
  date: string
  skins: ValorantSkin[]
}

export interface ValorantWeapon {
  id: number
  assetId: string
  name: string
  category: string
  defaultSkinAssetId: string
  displayIconUrl: string | null
}
