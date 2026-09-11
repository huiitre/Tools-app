export interface GameServer {
  slug: string
  gameCode: string
  hasDashboard: boolean
  gameName: string
  serverName: string
  pictureUrl: string | null
  online: boolean | null
  numPlayers: number | null
  maxPlayers: number | null
  checkedAt: string | null
  clientHost: string | null
  clientPort: number | null
  // Suffisent à décider si la carte propose les mods ; la liste est chargée à l'ouverture.
  modCount: number
  hasModpack: boolean
}

export interface GameServerMod {
  name: string
  version: string | null
  url: string | null
  authors: string[]
  fileName: string | null
  // Posée dans le manifest ou fournie par l'hébergeur du mod (Modrinth, CurseForge).
  iconUrl: string | null
}

export interface GameServerMods {
  // Null quand les joueurs n'ont rien à télécharger (jeu qui installe ses mods à la connexion).
  modpackUrl: string | null
  modpackSize: number | null
  mods: GameServerMod[]
}

export interface GameServerDetails {
  serverName: string
  gameName: string | null
  pictureUrl: string | null
  version: string | null
  description: string | null
  worldId: string | null
  // Clés et types propres à chaque jeu : rien n'est garanti au-delà du nom du réglage.
  settings: Record<string, unknown> | null
  // Déjà filtrées par l'API sur les droits de l'utilisateur : ce qui est là est autorisé.
  actions: GameServerAction[]
}

export interface GameServerActionParameter {
  name: string
  label: string
  // 'text' | 'number' | 'player' — 'player' se remplit avec les joueurs connectés.
  type: string
  required: boolean
  placeholder: string | null
}

export interface GameServerAction {
  code: string
  label: string
  icon: string
  dangerous: boolean
  parameters: GameServerActionParameter[]
}

export interface GameServerLiveCompanion {
  name: string
  level: number | null
  health: number | null
  maxHealth: number | null
}

export interface GameServerLivePlayer {
  name: string
  id: string | null
  ping: number | null
  level: number | null
  health: number | null
  maxHealth: number | null
  groupId: string | null
  groupName: string | null
  mapX: number | null
  mapY: number | null
  // Coordonnées brutes du monde : seules utilisables pour projeter sur une carte.
  positionX: number | null
  positionY: number | null
  companion: GameServerLiveCompanion | null
  // Monde ou dimension du joueur, pour un jeu qui en a plusieurs (Minecraft).
  world: string | null
}

export interface GameServerLiveStructure {
  key: string
  name: string
  groupId: string | null
  groupName: string | null
  positionX: number
  positionY: number
  // Créatures rattachées à la construction (les pals d'une base chez Palworld).
  creatureCount: number | null
}

// Tout est optionnel : un jeu qui n'expose pas une donnée renvoie null, et le front
// affiche « indisponible » plutôt que de masquer le bloc.
export interface GameServerLive {
  playerCount: number | null
  maxPlayers: number | null
  fps: number | null
  averageFps: number | null
  frameTimeMs: number | null
  uptimeSeconds: number | null
  inGameDay: number | null
  baseCount: number | null
  players: GameServerLivePlayer[]
  structures: GameServerLiveStructure[]
  log: string[]
  unavailable: string[]
}
