import type { TemtemSummary, TemtemTechnique } from '@/modules/Temtem/shared/types/temtem.types'

export interface TemtemTeamMember {
  id: number
  /** Place dans l'équipe, 1 à 6. */
  slot: number
  temtem: TemtemSummary
  /** Ce que le joueur a retenu, pas ce que le Temtem sait faire. */
  techniques: TemtemTechnique[]
}

export interface TemtemTeam {
  id: number
  name: string
  members: TemtemTeamMember[]
  createdAt: string
  updatedAt: string
}
