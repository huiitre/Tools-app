export interface PalworldTierGroup {
  tier: string
  palIds: number[]
}

export type PalworldTierListsByCategory = Record<string, PalworldTierGroup[]>
export type PalworldTierListsBySource = Record<string, PalworldTierListsByCategory>
