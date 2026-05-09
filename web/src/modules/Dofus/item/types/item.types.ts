import { AssetResolution } from './assetResolution.enum'

/* =========================
   ITEM TYPE
========================= */

export type ItemType = {
  id: number
  name: string
  assetId: number
  gameVersionId: number
}

/* =========================
   IMAGE
========================= */

type AssetImage = {
  id: number
  iconId: number
  resolution: AssetResolution
  url: string
}

export type ItemImage = AssetImage & {
  itemId: number
}

/* =========================
   ITEM PRICE
========================= */

export type ItemPrice = {

  itemId: number
  parentItemIds?: number[] | null

  userPrice: number
  communityAveragePrice: number
  lastUpdatedPrice: number
  
  craftUserPrice: number
  craftCommunityPrice: number
  craftLastPrice: number
  craftCalculatedPrice: number

  userPriceCreatedAt: Date | null
  communityAveragePriceCreatedAt: Date | null
  lastUpdatedPriceCreatedAt: Date | null
}

/* =========================
   FARMZONE
========================= */

export type MonsterImage = AssetImage & {
  monsterId: number
}

type Area = {
  id: number
  assetId: number
  name: string
}

type SubArea = {
  id: number
  areaId: number
  assetId: number
  name: string
}

type Monster = {
  id: number
  name: string
  images: MonsterImage[]
}

type FarmZone = {
  area: Area
  subarea: SubArea
  monsters: Monster[]
  isPrimary: boolean
}

/* =========================
   ITEM (DOMAINE)
========================= */

export type ItemLight = {
  id: number
  name: string
  level: number
  gameVersionId: number
  assetId: number
  itemType: ItemType
  images: ItemImage[]
}

export type Item = {
  id: number
  name: string
  description: string
  hasRecipe: boolean
  assetId: number
  gameVersionId: number
  level: number
  type: ItemType
  images: ItemImage[]

  parentItemId?: number | null
  quantity?: number | null
  farmZones?: FarmZone[]
}