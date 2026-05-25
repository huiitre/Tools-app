export type Team = 'RED' | 'BLUE'
export type Role = 'SPYMASTER' | 'OPERATIVE' | 'SPECTATOR'
export type CardColor = 'RED' | 'BLUE' | 'NEUTRAL' | 'ASSASSIN'
export type GameStatus = 'LOBBY' | 'IN_PROGRESS' | 'FINISHED'
export type ProposalStatus = 'PENDING' | 'VALIDATED' | 'REJECTED'

export interface CodenameCard {
  word: string
  color: CardColor
  revealed: boolean
}

export interface CodenamePlayer {
  id: string
  nickname: string
  team: Team | null
  role: Role
  isReady: boolean
  userId?: number
}

export interface CodenameClue {
  word: string
  count: number
}

export interface CodenameSession {
  id: string
  status: GameStatus
  board: CodenameCard[]
  currentTurn: Team | null
  scores: Record<Team, number>
  startingTeam: Team | null
  clue: CodenameClue | null
  players: CodenamePlayer[]
  winner?: Team
}

export interface CodenameWord {
  id: string
  content: string
  validated: boolean
  createdAt: string
  tags: CodenameTag[]
}

export interface CodenameTag {
  id: string
  label: string
}

export interface CodenameProposal {
  id: string
  content: string
  suggestedTags: string[]
  proposedBy: string | null
  status: ProposalStatus
  createdAt: string
}

export interface CodenameHistoryEntry {
  id: string
  sessionId: string
  team: Team
  role: Exclude<Role, 'SPECTATOR'>
  result: 'WIN' | 'LOSS'
  createdAt: string
}