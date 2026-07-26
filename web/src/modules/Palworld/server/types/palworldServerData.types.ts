export interface PalworldPlayerSummary {
  playerUid: string
  name: string
  lastOnlineRealTime: number | null
}

export interface PalworldBaseSummary {
  baseId: string
  palCount: number
}

export interface PalworldGuildSummary {
  guildId: string
  name: string
  players: PalworldPlayerSummary[]
  bases: PalworldBaseSummary[]
}

export interface PalworldPalInstanceSummary {
  instanceId: string
  characterId: string
  palId: number | null
  palName: string | null
  palImageUrl: string | null
  palFoodAmount: number | null
  isAlpha: boolean
  ownerPlayerUid: string | null
  level: number | null
  exp: number | null
  fullStomach: number | null
  isSick: boolean | null
  workableType: string | null
  taskId: string | null
  workState: number | null
  currentWorkAmount: number | null
  requiredWorkAmount: number | null
  firstSeenAt: string
  lastSeenAt: string
}

export interface PalworldPalInstanceSnapshot {
  capturedAt: string
  level: number | null
  exp: number | null
  fullStomach: number | null
  isSick: boolean | null
  workableType: string | null
  taskId: string | null
  workState: number | null
  currentWorkAmount: number | null
  requiredWorkAmount: number | null
}
