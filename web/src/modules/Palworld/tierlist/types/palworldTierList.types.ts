export interface PalworldPal {
  name: string
  image: string
  href: string
}

export interface PalworldTierGroup {
  tier: string
  pals: PalworldPal[]
}

export type PalworldTierListsByCategory = Record<string, PalworldTierGroup[]>
