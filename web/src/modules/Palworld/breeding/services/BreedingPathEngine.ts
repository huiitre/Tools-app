import type { BreedingPathNode, BreedingPathResult, BreedingPathRoute } from '../types/breeding.types'

export interface BreedingRuleData { parentAPalId: number; parentAGender: 'Male' | 'Female' | null; parentBPalId: number; parentBGender: 'Male' | 'Female' | null; childPalId: number }
type Pair = BreedingRuleData & { rule: 'exception' | 'formula' }
type Score = { breeds: number; depth: number }
type TargetDerivation = {
  state: string
  pair: Pair
  parentAState: string
  parentBState: string
  score: Score
  passiveCount: number
}
type DecodedState = {
  speciesId: number
  gender: BreedingOwnedPal['gender']
  mask: number
  storageLocation: BreedingOwnedPal['storageLocation']
}
const key = (a: number, b: number) => a < b ? `${a}:${b}` : `${b}:${a}`

export interface BreedingOwnedPal {
  speciesId: number
  passiveSkillIds: string[]
  gender: 'Male' | 'Female' | null
  storageLocation: 'base' | 'palbox' | 'party' | 'dimensional_storage' | null
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
    onProgress?: (progress: BreedingPathProgress) => void,
  ): BreedingPathResult {
    // Le catalogue et les Pals possédés viennent de deux sources distinctes : on ignore ici toute
    // espèce inconnue plutôt que de laisser la construction des nœuds échouer plus loin.
    if (!this.pals.has(target)) return { reachable: false, root: null, routes: [] }
    const ownedPals = (owned instanceof Set
      ? [...owned].map(speciesId => ({ speciesId, passiveSkillIds: [], gender: null, storageLocation: null }))
      : owned
    ).filter(ownedPal => this.pals.has(ownedPal.speciesId))
    const uniqueRequiredPassiveIds = [...new Set(requiredPassiveIds)]

    if (uniqueRequiredPassiveIds.length === 0 && ownedPals.some(pal => pal.gender !== null)) {
      const directRoutes = this.computeDirectTargetRoutes(target, ownedPals)
      if (directRoutes !== null) return directRoutes
    }

    if (uniqueRequiredPassiveIds.length === 0 && ownedPals.every(pal => pal.gender === null)) {
      return this.computeBySpecies(target, new Set(ownedPals.map(pal => pal.speciesId)), onProgress)
    }

    return this.computeWithPassives(target, ownedPals, uniqueRequiredPassiveIds, onProgress)
  }

  private computeDirectTargetRoutes(
    target: number,
    ownedPals: BreedingOwnedPal[],
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

    // Posséder la cible n'interrompt pas la recherche : on cherche toujours un couple, puisque
    // demander un calcul signifie vouloir en élever d'autres. Les Pals possédés de l'espèce
    // cible deviennent simplement des parents candidats comme les autres.
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

    // À égalité, un couple formé de deux Pals de l'espèce cible déjà possédés passe devant :
    // croiser X avec X garantit un X, là où un couple par formule dépend des rangs.
    const usesTargetPair = (pair: Pair) => pair.parentAPalId === target && pair.parentBPalId === target
    directCandidates.sort((left, right) => Number(usesTargetPair(right.pair)) - Number(usesTargetPair(left.pair)))

    const seenSpeciesPairs = new Set<string>()
    const distinctDirectCandidates = directCandidates.filter(({ pair }) => {
      const signature = key(pair.parentAPalId, pair.parentBPalId)
      if (seenSpeciesPairs.has(signature)) return false
      seenSpeciesPairs.add(signature)
      return true
    })

    const routes = distinctDirectCandidates.slice(0, 5).map(({ pair, parentA, parentB }) => {
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

    // Posséder la cible n'interrompt pas la recherche : elle devient un parent candidat parmi
    // les autres. Si aucun couple n'existe, le repli en fin de méthode la renvoie seule.
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

    // Moins d'étapes d'abord ; à égalité, un couple de deux Pals de l'espèce cible déjà
    // possédés passe devant, croiser X avec X garantissant un X.
    const usesOwnedTargetPair = (pair: Pair) => pair.parentAPalId === target
      && pair.parentBPalId === target
      && owned.has(target)
    const derivedRoutes = [...targetPairs.values()]
      .sort((left, right) => left.score.breeds - right.score.breeds
        || Number(usesOwnedTargetPair(right.pair)) - Number(usesOwnedTargetPair(left.pair))
        || left.score.depth - right.score.depth)
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
    onProgress?: (progress: BreedingPathProgress) => void,
  ): BreedingPathResult {
    const passiveBitById = new Map(requiredPassiveIds.map((id, index) => [id, 1 << index]))
    const respectGenders = ownedPals.some(pal => pal.gender !== null)
    const scoreByState = new Map<string, Score>()
    const fromByState = new Map<string, { pair: Pair; parentAState: string; parentBState: string }>()
    const targetDerivations = new Map<string, TargetDerivation>()
    const statesBySpecies = new Map<number, Set<string>>()
    const queue: string[] = []
    let exploredStates = 0

    // Les états sont décodés une seule fois à la création : la boucle d'exploration lit des
    // champs déjà typés au lieu de re-parser la clé (split + Number) à chaque transition.
    const decodedByState = new Map<string, DecodedState>()
    const stateKey = (
      speciesId: number,
      gender: BreedingOwnedPal['gender'],
      passiveMask: number,
      storageLocation: BreedingOwnedPal['storageLocation'],
    ) => {
      const state = `${speciesId}:${gender ?? 'Unknown'}:${passiveMask}:${storageLocation ?? 'Generated'}`
      if (!decodedByState.has(state)) {
        decodedByState.set(state, { speciesId, gender, mask: passiveMask, storageLocation })
      }
      return state
    }
    const decode = (state: string) => decodedByState.get(state)!
    const stateSpeciesId = (state: string) => decode(state).speciesId
    const stateGender = (state: string) => decode(state).gender
    const stateMask = (state: string) => decode(state).mask
    const stateStorageLocation = (state: string) => decode(state).storageLocation
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

    // Curseur de lecture plutôt que queue.shift() : shift() décale tout le tableau (O(n)) à
    // chaque état dépilé, ce qui devient dominant sur des dizaines de milliers d'états.
    const childGenderOptions = this.childGenders(respectGenders)
    let queueHead = 0
    while (queueHead < queue.length) {
      const currentState = queue[queueHead++]
      exploredStates += 1
      if (exploredStates % 250 === 0) {
        onProgress?.({ stage: 'exploring', exploredStates, candidateRoutes: targetDerivations.size })
      }
      const current = decode(currentState)
      const currentSpeciesId = current.speciesId
      const currentScore = scoreByState.get(currentState)!

      for (const pair of this.pairsByParent.get(currentSpeciesId) ?? []) {
        // Pour un auto-croisement (X + X = Y) les deux côtés matchent l'espèce courante :
        // on doit garder otherState comme second parent, sinon le couple se réduit au même
        // individu et devient toujours invalide (même genre).
        const currentIsParentA = pair.parentAPalId === currentSpeciesId
        const otherSpeciesId = currentIsParentA ? pair.parentBPalId : pair.parentAPalId
        const otherStates = statesBySpecies.get(otherSpeciesId)
        if (!otherStates) continue
        const isTargetChild = pair.childPalId === target

        for (const otherState of otherStates) {
          const other = decode(otherState)
          const parentAState = currentIsParentA ? currentState : otherState
          const parentBState = currentIsParentA ? otherState : currentState
          const parentAGender = currentIsParentA ? current.gender : other.gender
          const parentBGender = currentIsParentA ? other.gender : current.gender
          if (!this.canBreed(pair, parentAGender, parentBGender, respectGenders)) continue

          const otherScore = scoreByState.get(otherState)!
          const nextScore = {
            breeds: currentScore.breeds + otherScore.breeds + 1,
            depth: Math.max(currentScore.depth, otherScore.depth) + 1,
          }
          const childMask = current.mask | other.mask

          if (isTargetChild) {
            const signature = key(current.speciesId, other.speciesId)
            const currentDerivation = targetDerivations.get(signature)
            const nextPassiveCount = bitCount(childMask)
            const isPreferredDerivation = !currentDerivation
              || nextPassiveCount > currentDerivation.passiveCount
              || nextPassiveCount === currentDerivation.passiveCount && isBetter(nextScore, currentDerivation.score)
            if (isPreferredDerivation) {
              targetDerivations.set(signature, {
                state: stateKey(pair.childPalId, childGenderOptions[0], childMask, null),
                pair,
                parentAState,
                parentBState,
                score: nextScore,
                passiveCount: nextPassiveCount,
              })
            }
          }

          for (const childGender of childGenderOptions) {
            addState(pair.childPalId, childGender, childMask, null, nextScore, { pair, parentAState, parentBState })
          }
        }
      }
    }

    onProgress?.({ stage: 'building-routes', exploredStates, candidateRoutes: targetDerivations.size })
    const targetCandidates = [...(statesBySpecies.get(target) ?? [])]
    if (targetCandidates.length === 0) return { reachable: false, root: null, routes: [] }

    // Ce classement ne sert qu'au repli « aucun couple trouvé » plus bas : le maximum de passifs
    // d'abord, puis le moins d'étapes.
    targetCandidates.sort((left, right) => {
      const passiveCountDifference = bitCount(stateMask(right)) - bitCount(stateMask(left))
      if (passiveCountDifference !== 0) return passiveCountDifference
      const leftScore = scoreByState.get(left)!
      const rightScore = scoreByState.get(right)!
      return leftScore.breeds - rightScore.breeds || leftScore.depth - rightScore.depth
    })

    const passiveSkillIdsForState = (state: string) => requiredPassiveIds.filter((_, index) =>
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

    // Posséder la cible n'interrompt pas la recherche : les Pals possédés de l'espèce cible sont
    // des parents candidats, et le repli ci-dessous ne les renvoie seuls qu'à défaut de couple.
    const derivedRoutes = [...targetDerivations.values()]
      .sort((left, right) => this.compareTargetDerivations(left, right, target, fromByState))
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
        passiveCount: derivation.passiveCount,
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

  /**
   * Le maximum de passifs transmis prime : une route qui perd le passif recherché n'a aucun
   * intérêt, même en moins d'étapes. Vient ensuite le nombre d'étapes, puis — à égalité stricte —
   * un couple formé de deux Pals de l'espèce cible déjà possédés, croiser X avec X garantissant
   * un X là où un couple par formule dépend des rangs.
   */
  private compareTargetDerivations(
    left: TargetDerivation,
    right: TargetDerivation,
    target: number,
    fromByState: Map<string, { pair: Pair; parentAState: string; parentBState: string }>,
  ) {
    return right.passiveCount - left.passiveCount
      || left.score.breeds - right.score.breeds
      || Number(this.derivationUsesOwnedTargetPair(right, target, fromByState))
        - Number(this.derivationUsesOwnedTargetPair(left, target, fromByState))
      || left.score.depth - right.score.depth
  }

  private derivationUsesOwnedTargetPair(
    derivation: TargetDerivation,
    target: number,
    fromByState: Map<string, { pair: Pair; parentAState: string; parentBState: string }>,
  ) {
    return derivation.pair.parentAPalId === target
      && derivation.pair.parentBPalId === target
      && !fromByState.has(derivation.parentAState)
      && !fromByState.has(derivation.parentBState)
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
