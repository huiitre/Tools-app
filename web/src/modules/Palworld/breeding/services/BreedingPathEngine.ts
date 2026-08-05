import type { BreedingPathNode, BreedingPathResult, BreedingPathRoute } from '../types/breeding.types'

export interface BreedingRuleData { parentAPalId: number; parentAGender: 'Male' | 'Female' | null; parentBPalId: number; parentBGender: 'Male' | 'Female' | null; childPalId: number }
type Pair = BreedingRuleData & { rule: 'exception' | 'formula' }
type Score = { breeds: number; depth: number }
type TargetDerivation = { state: string; pair: Pair; parentAState: string; parentBState: string; score: Score }
const key = (a: number, b: number) => a < b ? `${a}:${b}` : `${b}:${a}`

export interface BreedingOwnedPal {
  speciesId: number
  passiveSkillIds: string[]
  gender: 'Male' | 'Female' | null
  storageLocation: 'base' | 'palbox' | 'party' | 'dimensional_storage' | null
}

export interface BreedingPathOptions {
  prioritizeTargetSpecies?: boolean
}

export interface BreedingPathPalDefinition {
  id: number
  tribe: string
  name: string
  combiRank: number | null
  combiDuplicatePriority: number | null
  ignoreCombi: boolean
}

export interface BreedingPathProgress {
  stage: 'exploring' | 'building-routes'
  exploredStates: number
  candidateRoutes: number
}

export class BreedingPathEngine {
  private readonly pals = new Map<number, BreedingPathPalDefinition>()
  private readonly pairsByParent = new Map<number, Pair[]>()
  private readonly pairsByChild = new Map<number, Pair[]>()

  constructor(pals: BreedingPathPalDefinition[], rules: BreedingRuleData[]) {
    if (pals.some(pal => pal.combiRank !== null && typeof pal.combiDuplicatePriority !== 'number')) {
      throw new Error('Le catalogue d’élevage est incomplet.')
    }
    pals.forEach(pal => this.pals.set(pal.id, pal))
    const covered = new Set<string>()
    rules.forEach(rule => { covered.add(key(rule.parentAPalId, rule.parentBPalId)); this.add({ ...rule, rule: 'exception' }) })
    const specialChildren = new Set(rules.map(rule => rule.childPalId))
    const candidatesByRank = new Map<number, BreedingPathPalDefinition>()
    for (const candidate of pals.filter(p => p.combiRank !== null && !p.ignoreCombi && !specialChildren.has(p.id))) {
      const current = candidatesByRank.get(candidate.combiRank!)
      if (!current || (candidate.combiDuplicatePriority ?? 0) > (current.combiDuplicatePriority ?? 0)) {
        candidatesByRank.set(candidate.combiRank!, candidate)
      }
    }
    const candidates = [...candidatesByRank.values()].sort((left, right) => left.combiRank! - right.combiRank!)
    const resolveFormulaChild = (rank: number) => {
      let low = 0
      let high = candidates.length
      while (low < high) {
        const middle = Math.floor((low + high) / 2)
        if (candidates[middle].combiRank! < rank) low = middle + 1
        else high = middle
      }
      const nearbyCandidates = [candidates[low - 1], candidates[low]].filter((candidate): candidate is BreedingPathPalDefinition => candidate !== undefined)
      return nearbyCandidates.reduce((best, candidate) => {
        if (!best) return candidate
        const candidateDistance = Math.abs(candidate.combiRank! - rank)
        const bestDistance = Math.abs(best.combiRank! - rank)
        return candidateDistance < bestDistance
          || candidateDistance === bestDistance && (candidate.combiDuplicatePriority ?? 0) > (best.combiDuplicatePriority ?? 0)
          ? candidate
          : best
      }, null as BreedingPathPalDefinition | null)
    }
    for (let i = 0; i < pals.length; i++) for (let j = i; j < pals.length; j++) {
      const a = pals[i]; const b = pals[j]
      if (a.combiRank === null || b.combiRank === null || covered.has(key(a.id, b.id))) continue
      const rank = Math.floor((a.combiRank + b.combiRank + 1) / 2)
      const child = resolveFormulaChild(rank)
      if (child) this.add({ parentAPalId: a.id, parentAGender: null, parentBPalId: b.id, parentBGender: null, childPalId: child.id, rule: 'formula' })
    }
  }

  private add(pair: Pair) {
    for (const id of new Set([pair.parentAPalId, pair.parentBPalId])) {
      const pairs = this.pairsByParent.get(id)
      if (pairs) pairs.push(pair)
      else this.pairsByParent.set(id, [pair])
    }
    const childPairs = this.pairsByChild.get(pair.childPalId)
    if (childPairs) childPairs.push(pair)
    else this.pairsByChild.set(pair.childPalId, [pair])
  }

  compute(
    target: number,
    owned: Set<number> | BreedingOwnedPal[],
    requiredPassiveIds: string[] = [],
    options: BreedingPathOptions = {},
    onProgress?: (progress: BreedingPathProgress) => void,
  ): BreedingPathResult {
    const ownedPals = owned instanceof Set
      ? [...owned].map(speciesId => ({ speciesId, passiveSkillIds: [], gender: null, storageLocation: null }))
      : owned
    const uniqueRequiredPassiveIds = [...new Set(requiredPassiveIds)]

    if (uniqueRequiredPassiveIds.length === 0 && ownedPals.some(pal => pal.gender !== null)) {
      const directRoutes = this.computeDirectTargetRoutes(target, ownedPals, options)
      if (directRoutes !== null) return directRoutes
    }

    if (uniqueRequiredPassiveIds.length === 0 && ownedPals.every(pal => pal.gender === null)) {
      return this.computeBySpecies(target, new Set(ownedPals.map(pal => pal.speciesId)), onProgress)
    }

    return this.computeWithPassives(target, ownedPals, uniqueRequiredPassiveIds, options, onProgress)
  }

  private computeDirectTargetRoutes(
    target: number,
    ownedPals: BreedingOwnedPal[],
    options: BreedingPathOptions,
  ): BreedingPathResult | null {
    const ownedBySpecies = new Map<number, BreedingOwnedPal[]>()
    for (const ownedPal of ownedPals) {
      if (!this.pals.has(ownedPal.speciesId)) continue
      const candidates = ownedBySpecies.get(ownedPal.speciesId) ?? []
      const signature = `${ownedPal.gender}:${ownedPal.storageLocation ?? 'Unknown'}`
      if (!candidates.some(candidate => `${candidate.gender}:${candidate.storageLocation ?? 'Unknown'}` === signature)) {
        candidates.push(ownedPal)
      }
      ownedBySpecies.set(ownedPal.speciesId, candidates)
    }

    const directCandidates: { pair: Pair; parentA: BreedingOwnedPal; parentB: BreedingOwnedPal }[] = []
    for (const pair of this.pairsByChild.get(target) ?? []) {
      for (const parentA of ownedBySpecies.get(pair.parentAPalId) ?? []) {
        for (const parentB of ownedBySpecies.get(pair.parentBPalId) ?? []) {
          if (!this.canBreed(pair, parentA.gender, parentB.gender, true)) continue
          directCandidates.push({ pair, parentA, parentB })
        }
      }
    }
    if (directCandidates.length === 0) return null

    directCandidates.sort((left, right) => {
      if (!options.prioritizeTargetSpecies) return 0
      const leftUsesTargetPair = left.pair.parentAPalId === target && left.pair.parentBPalId === target
      const rightUsesTargetPair = right.pair.parentAPalId === target && right.pair.parentBPalId === target
      return leftUsesTargetPair === rightUsesTargetPair ? 0 : leftUsesTargetPair ? -1 : 1
    })

    const nodeFromOwned = (ownedPal: BreedingOwnedPal): BreedingPathNode => {
      const pal = this.pals.get(ownedPal.speciesId)!
      return {
        species: { id: pal.id, tribe: pal.tribe, name: pal.name, combiRank: pal.combiRank },
        owned: true,
        gender: ownedPal.gender,
        storageLocation: ownedPal.storageLocation,
        passiveSkillIds: [],
        step: null,
      }
    }
    const routes = directCandidates.slice(0, 5).map(({ pair, parentA, parentB }) => {
      const targetPal = this.pals.get(target)!
      const root: BreedingPathNode = {
        species: { id: targetPal.id, tribe: targetPal.tribe, name: targetPal.name, combiRank: targetPal.combiRank },
        owned: false,
        gender: null,
        storageLocation: null,
        passiveSkillIds: [],
        step: {
          parentA: nodeFromOwned(parentA),
          parentB: nodeFromOwned(parentB),
          parentAGender: pair.parentAGender,
          parentBGender: pair.parentBGender,
          rule: pair.rule,
        },
      }
      return {
        id: `direct:${pair.parentAPalId}:${parentA.gender}:${pair.parentBPalId}:${parentB.gender}`,
        root,
        breeds: 1,
        passiveCount: 0,
      } satisfies BreedingPathRoute
    })

    return { reachable: true, root: routes[0].root, routes }
  }

  private computeBySpecies(
    target: number,
    owned: Set<number>,
    onProgress?: (progress: BreedingPathProgress) => void,
  ): BreedingPathResult {
    const node = (id: number, isOwned = owned.has(id)): BreedingPathNode => {
      const pal = this.pals.get(id)!
      return {
        species: { id, tribe: pal.tribe, name: pal.name, combiRank: pal.combiRank },
        owned: isOwned,
        gender: null,
        storageLocation: null,
        passiveSkillIds: [],
        step: null,
      }
    }

    if (owned.has(target)) {
      const ownedTarget = node(target)
      const root = {
        ...node(target, false),
        step: { parentA: ownedTarget, parentB: ownedTarget, parentAGender: null, parentBGender: null, rule: 'formula' as const },
      }
      return {
        reachable: true,
        root,
        routes: [{ id: `owned-target:${target}`, root, breeds: 1, passiveCount: 0 }],
      }
    }

    const score = new Map<number, Score>()
    const from = new Map<number, Pair>()
    const targetPairs = new Map<string, { pair: Pair; score: Score }>()
    const queue = [...owned]
    let exploredStates = 0
    owned.forEach(id => score.set(id, { breeds: 0, depth: 0 }))

    while (queue.length) {
      const id = queue.shift()!
      exploredStates += 1
      if (exploredStates % 250 === 0) {
        onProgress?.({ stage: 'exploring', exploredStates, candidateRoutes: targetPairs.size })
      }
      for (const pair of this.pairsByParent.get(id) ?? []) {
        const parentA = score.get(pair.parentAPalId)
        const parentB = score.get(pair.parentBPalId)
        if (!parentA || !parentB) continue

        const next = {
          breeds: parentA.breeds + parentB.breeds + 1,
          depth: Math.max(parentA.depth, parentB.depth) + 1,
        }
        const current = score.get(pair.childPalId)
        if (pair.childPalId === target) {
          const signature = key(pair.parentAPalId, pair.parentBPalId)
          const currentTargetPair = targetPairs.get(signature)
          if (!currentTargetPair || next.breeds < currentTargetPair.score.breeds
            || next.breeds === currentTargetPair.score.breeds && next.depth < currentTargetPair.score.depth) {
            targetPairs.set(signature, { pair, score: next })
          }
        }
        if (current && (current.breeds < next.breeds || current.breeds === next.breeds && current.depth <= next.depth)) continue

        score.set(pair.childPalId, next)
        from.set(pair.childPalId, pair)
        queue.push(pair.childPalId)
      }
    }

    onProgress?.({ stage: 'building-routes', exploredStates, candidateRoutes: targetPairs.size })
    if (!score.has(target)) return { reachable: false, root: null, routes: [] }

    const buildNode = (id: number): BreedingPathNode => {
      const pair = from.get(id)
      if (!pair) return node(id)
      return {
        ...node(id, false),
        step: {
          parentA: buildNode(pair.parentAPalId),
          parentB: buildNode(pair.parentBPalId),
          parentAGender: pair.parentAGender,
          parentBGender: pair.parentBGender,
          rule: pair.rule,
        },
      }
    }

    const derivedRoutes = [...targetPairs.values()]
      .sort((left, right) => left.score.breeds - right.score.breeds || left.score.depth - right.score.depth)
      .slice(0, 5)
      .map(({ pair, score: routeScore }) => {
        const root = {
          ...node(target, false),
          step: {
            parentA: buildNode(pair.parentAPalId),
            parentB: buildNode(pair.parentBPalId),
            parentAGender: pair.parentAGender,
            parentBGender: pair.parentBGender,
            rule: pair.rule,
          },
        }
        return {
          id: `species:${pair.parentAPalId}:${pair.parentBPalId}`,
          root,
          breeds: routeScore.breeds,
          passiveCount: 0,
        } satisfies BreedingPathRoute
      })
    const root = derivedRoutes[0]?.root ?? buildNode(target)
    return {
      reachable: true,
      root,
      routes: derivedRoutes.length > 0
        ? derivedRoutes
        : [{ id: `species:${target}`, root, breeds: score.get(target)!.breeds, passiveCount: 0 }],
    }
  }

  private computeWithPassives(
    target: number,
    ownedPals: BreedingOwnedPal[],
    requiredPassiveIds: string[],
    options: BreedingPathOptions,
    onProgress?: (progress: BreedingPathProgress) => void,
  ): BreedingPathResult {
    const passiveBitById = new Map(requiredPassiveIds.map((id, index) => [id, 1 << index]))
    const allPassivesMask = (1 << requiredPassiveIds.length) - 1
    const respectGenders = ownedPals.some(pal => pal.gender !== null)
    const scoreByState = new Map<string, Score>()
    const fromByState = new Map<string, { pair: Pair; parentAState: string; parentBState: string }>()
    const targetDerivations = new Map<string, TargetDerivation>()
    const statesBySpecies = new Map<number, Set<string>>()
    const queue: string[] = []
    let exploredStates = 0

    const stateKey = (
      speciesId: number,
      gender: BreedingOwnedPal['gender'],
      passiveMask: number,
      storageLocation: BreedingOwnedPal['storageLocation'],
    ) => `${speciesId}:${gender ?? 'Unknown'}:${passiveMask}:${storageLocation ?? 'Generated'}`
    const stateParts = (state: string) => state.split(':')
    const stateSpeciesId = (state: string) => Number(stateParts(state)[0])
    const stateGender = (state: string): BreedingOwnedPal['gender'] => {
      const gender = stateParts(state)[1]
      return gender === 'Male' || gender === 'Female' ? gender : null
    }
    const stateMask = (state: string) => Number(stateParts(state)[2])
    const stateStorageLocation = (state: string): BreedingOwnedPal['storageLocation'] => {
      const storageLocation = stateParts(state)[3]
      return storageLocation === 'base' || storageLocation === 'palbox' || storageLocation === 'party'
        || storageLocation === 'dimensional_storage'
        ? storageLocation
        : null
    }
    const isBetter = (next: Score, current: Score | undefined) => !current
      || next.breeds < current.breeds
      || (next.breeds === current.breeds && next.depth < current.depth)

    const addState = (
      speciesId: number,
      gender: BreedingOwnedPal['gender'],
      passiveMask: number,
      storageLocation: BreedingOwnedPal['storageLocation'],
      score: Score,
      from?: { pair: Pair; parentAState: string; parentBState: string },
    ) => {
      const state = stateKey(speciesId, gender, passiveMask, storageLocation)
      if (!isBetter(score, scoreByState.get(state))) return

      scoreByState.set(state, score)
      if (from) fromByState.set(state, from)
      else fromByState.delete(state)
      if (!statesBySpecies.has(speciesId)) statesBySpecies.set(speciesId, new Set())
      statesBySpecies.get(speciesId)!.add(state)
      queue.push(state)
    }

    for (const ownedPal of ownedPals) {
      if (!this.pals.has(ownedPal.speciesId)) continue
      const passiveMask = ownedPal.passiveSkillIds.reduce(
        (mask, passiveId) => mask | (passiveBitById.get(passiveId) ?? 0),
        0,
      )
      addState(ownedPal.speciesId, ownedPal.gender, passiveMask, ownedPal.storageLocation, { breeds: 0, depth: 0 })
    }

    while (queue.length) {
      const currentState = queue.shift()!
      exploredStates += 1
      if (exploredStates % 250 === 0) {
        onProgress?.({ stage: 'exploring', exploredStates, candidateRoutes: targetDerivations.size })
      }
      const currentSpeciesId = stateSpeciesId(currentState)
      const currentScore = scoreByState.get(currentState)!

      for (const pair of this.pairsByParent.get(currentSpeciesId) ?? []) {
        const otherSpeciesId = pair.parentAPalId === currentSpeciesId ? pair.parentBPalId : pair.parentAPalId
        for (const otherState of statesBySpecies.get(otherSpeciesId) ?? []) {
          const otherScore = scoreByState.get(otherState)!
          const currentIsParentA = pair.parentAPalId === currentSpeciesId
          const currentIsParentB = pair.parentBPalId === currentSpeciesId
          const parentAState = currentIsParentA ? currentState : otherState
          const parentBState = currentIsParentB ? currentState : otherState
          if (!this.canBreed(pair, stateGender(parentAState), stateGender(parentBState), respectGenders)) continue
          const nextScore = {
            breeds: currentScore.breeds + otherScore.breeds + 1,
            depth: Math.max(currentScore.depth, otherScore.depth) + 1,
          }
          for (const childGender of this.childGenders(respectGenders)) {
            const childMask = stateMask(parentAState) | stateMask(parentBState)
            const childState = stateKey(pair.childPalId, childGender, childMask, null)
            if (pair.childPalId === target) {
              const signature = [parentAState, parentBState].sort().join('|')
              const currentDerivation = targetDerivations.get(signature)
              if (!currentDerivation || isBetter(nextScore, currentDerivation.score)) {
                targetDerivations.set(signature, { state: childState, pair, parentAState, parentBState, score: nextScore })
              }
            }
            addState(
              pair.childPalId,
              childGender,
              childMask,
              null,
              nextScore,
              { pair, parentAState, parentBState },
            )
          }
        }
      }
    }

    onProgress?.({ stage: 'building-routes', exploredStates, candidateRoutes: targetDerivations.size })
    const targetStates = [...(statesBySpecies.get(target) ?? [])]
    if (targetStates.length === 0) return { reachable: false, root: null, routes: [] }

    const targetStatesProducedByBreeding = targetStates.filter(state => fromByState.has(state))
    const targetCandidates = targetStatesProducedByBreeding.length > 0
      ? targetStatesProducedByBreeding
      : targetStates

    targetCandidates.sort((left, right) => {
      const passiveCountDifference = bitCount(stateMask(right)) - bitCount(stateMask(left))
      if (passiveCountDifference !== 0) return passiveCountDifference
      if (options.prioritizeTargetSpecies) {
        const leftUsesOwnedTargetPair = this.usesOwnedTargetPair(left, target, fromByState)
        const rightUsesOwnedTargetPair = this.usesOwnedTargetPair(right, target, fromByState)
        if (leftUsesOwnedTargetPair !== rightUsesOwnedTargetPair) return leftUsesOwnedTargetPair ? -1 : 1
      }
      const leftScore = scoreByState.get(left)!
      const rightScore = scoreByState.get(right)!
      return leftScore.breeds - rightScore.breeds || leftScore.depth - rightScore.depth
    })

    const passiveSkillIdsForState = (state: string) => requiredPassiveIds.filter((id, index) =>
      (stateMask(state) & (1 << index)) !== 0)

    const node = (
      speciesId: number,
      owned: boolean,
      gender: BreedingOwnedPal['gender'],
      storageLocation: BreedingOwnedPal['storageLocation'],
      passiveSkillIds: string[],
    ): BreedingPathNode => {
      const pal = this.pals.get(speciesId)!
      return {
        species: { id: speciesId, tribe: pal.tribe, name: pal.name, combiRank: pal.combiRank },
        owned,
        gender,
        storageLocation,
        passiveSkillIds,
        step: null,
      }
    }

    const buildNode = (state: string): BreedingPathNode => {
      const speciesId = stateSpeciesId(state)
      const gender = stateGender(state)
      const storageLocation = stateStorageLocation(state)
      const from = fromByState.get(state)
      const passiveSkillIds = passiveSkillIdsForState(state)
      if (!from) return node(speciesId, true, gender, storageLocation, passiveSkillIds)

      return {
        ...node(speciesId, false, gender, storageLocation, passiveSkillIds),
        step: {
          parentA: buildNode(from.parentAState),
          parentB: buildNode(from.parentBState),
          parentAGender: from.pair.parentAGender,
          parentBGender: from.pair.parentBGender,
          rule: from.pair.rule,
        },
      }
    }

    const selectedState = targetCandidates[0]
    const passiveCoverage = stateMask(selectedState)
    if (!respectGenders && passiveCoverage === allPassivesMask && !fromByState.has(selectedState)) {
      const passiveSkillIds = passiveSkillIdsForState(selectedState)
      const ownedTarget = node(target, true, null, null, passiveSkillIds)
      const root = {
        ...node(target, false, null, null, passiveSkillIds),
        step: { parentA: ownedTarget, parentB: ownedTarget, parentAGender: null, parentBGender: null, rule: 'formula' as const },
      }
      return {
        reachable: true,
        root,
        routes: [{
          id: `owned-target:${selectedState}`,
          root,
          breeds: 1,
          passiveCount: bitCount(passiveCoverage),
        }],
      }
    }

    const derivedRoutes = [...targetDerivations.values()]
      .sort((left, right) => this.compareTargetCandidates(
        left.state,
        right.state,
        left.score,
        right.score,
        left.pair,
        right.pair,
        left.parentAState,
        left.parentBState,
        right.parentAState,
        right.parentBState,
        target,
        options.prioritizeTargetSpecies,
        fromByState,
      ))
      .slice(0, 5)
      .map(derivation => ({
        id: `${derivation.state}:${derivation.parentAState}:${derivation.parentBState}`,
        root: {
          ...node(target, false, stateGender(derivation.state), null, passiveSkillIdsForState(derivation.state)),
          step: {
            parentA: buildNode(derivation.parentAState),
            parentB: buildNode(derivation.parentBState),
            parentAGender: derivation.pair.parentAGender,
            parentBGender: derivation.pair.parentBGender,
            rule: derivation.pair.rule,
          },
        },
        breeds: derivation.score.breeds,
        passiveCount: bitCount(stateMask(derivation.state)),
      } satisfies BreedingPathRoute))

    const routes = derivedRoutes.length > 0
      ? derivedRoutes
      : this.distinctTargetStates(targetCandidates, fromByState)
        .slice(0, 5)
        .map(state => ({
          id: state,
          root: buildNode(state),
          breeds: scoreByState.get(state)!.breeds,
          passiveCount: bitCount(stateMask(state)),
        } satisfies BreedingPathRoute))

    return { reachable: true, root: routes[0].root, routes }
  }

  private canBreed(
    pair: Pair,
    parentAGender: BreedingOwnedPal['gender'],
    parentBGender: BreedingOwnedPal['gender'],
    respectGenders: boolean,
  ) {
    if (!respectGenders) return true
    if (parentAGender === null || parentBGender === null || parentAGender === parentBGender) return false
    return (pair.parentAGender === null || pair.parentAGender === parentAGender)
      && (pair.parentBGender === null || pair.parentBGender === parentBGender)
  }

  private childGenders(respectGenders: boolean): BreedingOwnedPal['gender'][] {
    return respectGenders ? ['Male', 'Female'] : [null]
  }

  private usesOwnedTargetPair(
    state: string,
    target: number,
    fromByState: Map<string, { pair: Pair; parentAState: string; parentBState: string }>,
  ) {
    const from = fromByState.get(state)
    return from?.pair.parentAPalId === target
      && from.pair.parentBPalId === target
      && !fromByState.has(from.parentAState)
      && !fromByState.has(from.parentBState)
  }

  private distinctTargetStates(
    states: string[],
    fromByState: Map<string, { pair: Pair; parentAState: string; parentBState: string }>,
  ) {
    const seenSignatures = new Set<string>()
    return states.filter(state => {
      const from = fromByState.get(state)
      const signature = from
        ? [from.parentAState, from.parentBState].sort().join('|')
        : state
      if (seenSignatures.has(signature)) return false
      seenSignatures.add(signature)
      return true
    })
  }

  private compareTargetCandidates(
    leftState: string,
    rightState: string,
    leftScore: Score,
    rightScore: Score,
    leftPair: Pair,
    rightPair: Pair,
    leftParentAState: string,
    leftParentBState: string,
    rightParentAState: string,
    rightParentBState: string,
    target: number,
    prioritizeTargetSpecies: boolean | undefined,
    fromByState: Map<string, { pair: Pair; parentAState: string; parentBState: string }>,
  ) {
    const passiveCountDifference = bitCount(Number(rightState.split(':')[2])) - bitCount(Number(leftState.split(':')[2]))
    if (passiveCountDifference !== 0) return passiveCountDifference
    if (prioritizeTargetSpecies) {
      const leftUsesOwnedTargetPair = leftPair.parentAPalId === target
        && leftPair.parentBPalId === target
        && !fromByState.has(leftParentAState)
        && !fromByState.has(leftParentBState)
      const rightUsesOwnedTargetPair = rightPair.parentAPalId === target
        && rightPair.parentBPalId === target
        && !fromByState.has(rightParentAState)
        && !fromByState.has(rightParentBState)
      if (leftUsesOwnedTargetPair !== rightUsesOwnedTargetPair) return leftUsesOwnedTargetPair ? -1 : 1
    }
    return leftScore.breeds - rightScore.breeds || leftScore.depth - rightScore.depth
  }
}

function bitCount(value: number): number {
  let count = 0
  let remaining = value
  while (remaining) {
    count += remaining & 1
    remaining >>>= 1
  }
  return count
}
