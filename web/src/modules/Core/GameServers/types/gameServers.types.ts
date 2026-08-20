export interface GameServer {
  gameName: string
  serverName: string
  pictureUrl: string | null
  online: boolean | null
  numPlayers: number | null
  maxPlayers: number | null
  checkedAt: string | null
  clientHost: string | null
  clientPort: number | null
}
