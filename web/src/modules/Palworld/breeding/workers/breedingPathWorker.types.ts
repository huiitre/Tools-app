import type {
  BreedingOwnedPal,
  BreedingPathPalDefinition,
  BreedingRuleData,
} from '../services/BreedingPathEngine'

export interface BreedingPathWorkerInput {
  pals: BreedingPathPalDefinition[]
  rules: BreedingRuleData[]
  targetId: number
  ownedPals: BreedingOwnedPal[]
  passiveSkillIds: string[]
}
