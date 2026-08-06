export interface PalworldPlayerSummary {
  playerUid: string
  name: string
  lastOnlineRealTime: number | null
}

export interface PalworldBaseSummary {
  baseId: string
  palCount: number
  positionX: number | null
  positionY: number | null
  positionZ: number | null
  rotationX: number | null
  rotationY: number | null
  rotationZ: number | null
  rotationW: number | null
  areaRange: number | null
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

export type PalworldPalStorageLocation = 'base' | 'palbox' | 'party' | 'dimensional_storage'

export interface PalworldServerPalInventory {
  instanceId: string
  palId: number | null
  ownerPlayerUid: string | null
  baseId: string | null
  storageLocation: PalworldPalStorageLocation
  containerId: string | null
  gender: 'male' | 'female' | null
  favoriteIndex: number | null
  passiveSkillIds: string[]
  rank: number
  ivHp: number | null
  ivAttack: number | null
  ivDefense: number | null
  currentHp: number | null
  baseHp: number | null
  baseMeleeAttack: number | null
  baseShotAttack: number | null
  baseDefense: number | null
  baseSupport: number | null
  baseCraftSpeed: number | null
  baseWorkSuitability: Record<string, number>
  workSuitabilityAddRanks: Record<string, number>
  level: number | null
  lastSeenAt: string
}

export interface PalworldServerInventory {
  lastSyncedAt: string | null
  guilds: PalworldGuildSummary[]
  pals: PalworldServerPalInventory[]
}
