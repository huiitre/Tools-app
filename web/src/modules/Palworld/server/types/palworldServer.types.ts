export interface PalworldServerInfo {
  version: string
  servername: string
  description: string
  worldguid: string
}

export interface PalworldServerPlayer {
  name: string
  accountName: string
  playerId: string
  userId: string
  ip: string
  ping: number
  locationX: number
  locationY: number
  mapX: number
  mapY: number
  level: number
}

export interface PalworldServerMetrics {
  currentPlayerNum: number
  serverFps: number
  serverFpsAverage: number
  serverFrameTime: number
  days: number
  maxPlayerNum: number
  baseCampNum: number
  uptime: number
}

export type PalworldServerSettings = Record<string, unknown>

export interface PalworldActivePal {
  name: string
  characterClass: string
  level: number
  hp: number
  maxHp: number
}

export interface PalworldGamePlayer {
  name: string
  userId: string
  ip: string
  level: number
  hp: number
  maxHp: number
  guildId: string
  guildName: string
  locationX: number
  locationY: number
  locationZ: number
  mapX: number
  mapY: number
  rotationZ: number
  activePal: PalworldActivePal | null
}

export interface PalworldBase {
  name: string
  guildId: string
  guildName: string
  locationX: number
  locationY: number
  locationZ: number
  mapX: number
  mapY: number
}

export interface PalworldBasePal {
  name: string
  characterClass: string
  level: number
  hp: number
  maxHp: number
  guildId: string
  locationX: number
  locationY: number
  locationZ: number
  mapX: number
  mapY: number
}

export interface PalworldGameData {
  time: string
  fps: number
  averageFps: number
  inGameTime: string
  inGameDays: number
  players: PalworldGamePlayer[]
  bases: PalworldBase[]
  basePals: PalworldBasePal[]
}
