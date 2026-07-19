export interface PalworldWorkSkill {
  name: string
  icon: string
  level: number
}

export interface PalworldSpeed {
  min: number
  max: number
}

export interface PalworldPal {
  name: string
  image: string
  href: string
  workSkills: PalworldWorkSkill[] | null
  speed: PalworldSpeed | null
}

export interface PalworldTierGroup {
  tier: string
  pals: PalworldPal[]
}

export type PalworldTierListsByCategory = Record<string, PalworldTierGroup[]>
