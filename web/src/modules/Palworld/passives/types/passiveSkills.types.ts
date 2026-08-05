export interface PalworldPassiveSkill {
  id: string
  name: string
  description: string | null
  rank: number
  rankIconUrl: string | null
  negative: boolean
  worldTree: boolean
}
