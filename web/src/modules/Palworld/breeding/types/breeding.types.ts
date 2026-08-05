export interface BreedingSpeciesRef {
  id: number
  tribe: string
  name: string
  combiRank: number | null
}

export interface BreedingFormulaDetails {
  parentARank: number
  parentBRank: number
  targetRank: number
  distance: number
}

export interface BreedingExceptionDetails {
  parentAGender: 'Male' | 'Female' | null
  parentBGender: 'Male' | 'Female' | null
}

export interface BreedingResult {
  parentA: BreedingSpeciesRef
  parentB: BreedingSpeciesRef
  child: BreedingSpeciesRef
  rule: 'exception' | 'formula'
  formula: BreedingFormulaDetails | null
  exception: BreedingExceptionDetails | null
}

export interface BreedingCombination {
  parentA: BreedingSpeciesRef
  parentB: BreedingSpeciesRef
  parentAGender: 'Male' | 'Female' | null
  parentBGender: 'Male' | 'Female' | null
  rule: 'exception' | 'formula'
  formula: BreedingFormulaDetails | null
  child: BreedingSpeciesRef
}

export type BreedingSearchMode = 'child' | 'parent'

export interface BreedingPathStep {
  parentA: BreedingPathNode
  parentB: BreedingPathNode
  parentAGender: 'Male' | 'Female' | null
  parentBGender: 'Male' | 'Female' | null
  rule: 'exception' | 'formula'
}

export interface BreedingPathNode {
  species: BreedingSpeciesRef
  owned: boolean
  gender: 'Male' | 'Female' | null
  storageLocation: 'base' | 'palbox' | 'party' | 'dimensional_storage' | null
  passiveSkillIds: string[]
  step: BreedingPathStep | null
}

export interface BreedingPathResult {
  reachable: boolean
  root: BreedingPathNode | null
  routes: BreedingPathRoute[]
}

export interface BreedingPathRoute {
  id: string
  root: BreedingPathNode
  breeds: number
  passiveCount: number
}
