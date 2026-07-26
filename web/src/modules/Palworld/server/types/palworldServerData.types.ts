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
