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
  level: number
  buildingCount: number
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
