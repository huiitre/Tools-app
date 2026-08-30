import type { TemtemSummary, TemtemType } from './types/temtem.types'

/** Numéro de Temtemdex tel qu'il s'affiche : l'identifiant EST le numéro du jeu. */
export function dexNumber(temtem: TemtemSummary): string {
  return `#${String(temtem.id).padStart(3, '0')}`
}

/** Les types d'un Temtem, un ou deux — le second est nul pour 79 d'entre eux. */
export function typesOf(temtem: TemtemSummary): TemtemType[] {
  return temtem.type2 ? [temtem.type1, temtem.type2] : [temtem.type1]
}
