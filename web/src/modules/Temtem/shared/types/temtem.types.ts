/**
 * Vues partagées du module Temtem, calquées une pour une sur celles de l'API.
 *
 * `TemtemSummary` est LA vue réutilisée partout — carte du Temtemdex, vignette d'équipe, ligne du
 * simulateur. Ne pas en dériver une variante par écran : côté API non plus il n'y en a qu'une.
 */

export interface TemtemType {
  id: number
  slug: string
  name: string
  imageUrl: string | null
}

export interface TemtemStats {
  hp: number
  stamina: number
  speed: number
  attack: number
  defense: number
  specialAttack: number
  specialDefense: number
}

export interface TemtemSummary {
  id: number
  slug: string
  name: string
  imageUrl: string | null
  type1: TemtemType
  /** Nul pour les 79 Temtem à type unique. */
  type2: TemtemType | null
  stats: TemtemStats
}

export interface TemtemCategory {
  code: string
  label: string
  imageUrl: string | null
}

export interface TemtemPriority {
  order: number
  label: string
  imageUrl: string | null
}

export interface TemtemTechnique {
  id: number
  slug: string
  name: string
  effect: string | null
  type: TemtemType
  category: TemtemCategory
  priority: TemtemPriority
  /** Nul pour une technique de statut — à ne pas confondre avec 0 dégât. */
  damage: number | null
  stamina: number | null
  /** Tours de chargement avant de pouvoir l'utiliser ; nul si disponible tout de suite. */
  chargeTurns: number | null
  targets: string[]
}

export interface TemtemTrait {
  id: number
  slug: string
  name: string
  effect: string | null
}

export interface TemtemLearnedTechnique {
  technique: TemtemTechnique
  /** LEVEL, BREEDING ou TRAINING. */
  source: string
  /** Renseigné seulement pour source = LEVEL. */
  level: number | null
}

export interface TemtemDetail {
  temtem: TemtemSummary
  techniques: TemtemLearnedTechnique[]
  traits: TemtemTrait[]
}
