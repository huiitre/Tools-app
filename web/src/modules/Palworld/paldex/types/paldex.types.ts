export interface PalworldElementSummary {
  id: number
  name: string
  iconUrl: string | null
}

export interface PalworldWorkSuitabilitySummary {
  id: number
  slug: string
  name: string
  iconUrl: string | null
  level: number
}

export interface PalworldPalListItem {
  id: number
  tribe: string
  paldexIndex: number | null
  paldexSuffix: string | null
  name: string
  imageUrl: string | null
  rarity: number | null
  size: string | null
  baseHp: number | null
  baseAttack: number | null
  baseDefense: number | null
  baseWorkSpeed: number | null
  baseSupport: number | null
  foodAmount: number | null
  bestWorkSuitabilityLabel: string | null
  elements: PalworldElementSummary[]
  workSuitabilities: PalworldWorkSuitabilitySummary[]
}

export type PaldexSortKey = 'paldex' | 'name' | 'hp' | 'attack' | 'defense' | 'workSpeed'
