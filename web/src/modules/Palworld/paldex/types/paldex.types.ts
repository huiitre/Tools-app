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

export interface PalworldPassiveSkillSummary {
  id: number
  name: string
  tooltip: string | null
  rankIconUrl: string | null
}

export interface PalworldPartnerSkillSummary {
  title: string
  description: string | null
  iconUrl: string | null
}

export interface PalworldPalListItem {
  id: number
  tribe: string
  paldexIndex: number | null
  paldexSuffix: string | null
  name: string
  imageUrl: string | null
  description: string | null
  rarity: number | null
  size: string | null
  baseHp: number | null
  baseAttack: number | null
  baseDefense: number | null
  baseWorkSpeed: number | null
  baseSupport: number | null
  foodAmount: number | null
  runSpeed: number | null
  rideSprintSpeed: number | null
  bestWorkSuitabilityLabel: string | null
  elements: PalworldElementSummary[]
  workSuitabilities: PalworldWorkSuitabilitySummary[]
  passiveSkills: PalworldPassiveSkillSummary[]
  partnerSkill: PalworldPartnerSkillSummary | null
}

export type PaldexSortKey = 'paldex' | 'name' | 'hp' | 'attack' | 'defense' | 'workSpeed'
