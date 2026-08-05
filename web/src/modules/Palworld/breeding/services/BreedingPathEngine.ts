import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingPathNode, BreedingPathResult } from '../types/breeding.types'

export interface BreedingRuleData { parentAPalId: number; parentAGender: 'Male' | 'Female' | null; parentBPalId: number; parentBGender: 'Male' | 'Female' | null; childPalId: number }
type Pair = BreedingRuleData & { rule: 'exception' | 'formula' }
type Score = { breeds: number; depth: number }
const key = (a: number, b: number) => a < b ? `${a}:${b}` : `${b}:${a}`

export class BreedingPathEngine {
  private readonly pals = new Map<number, PalworldPalListItem>()
  private readonly pairsByParent = new Map<number, Pair[]>()

  constructor(pals: PalworldPalListItem[], rules: BreedingRuleData[]) {
    if (pals.some(pal => pal.combiRank !== null && typeof pal.combiDuplicatePriority !== 'number')) {
      throw new Error('Le catalogue d’élevage est incomplet.')
    }
    pals.forEach(pal => this.pals.set(pal.id, pal))
    const covered = new Set<string>()
    rules.forEach(rule => { covered.add(key(rule.parentAPalId, rule.parentBPalId)); this.add({ ...rule, rule: 'exception' }) })
    const specialChildren = new Set(rules.map(rule => rule.childPalId))
    const candidates = pals.filter(p => p.combiRank !== null && !p.ignoreCombi && !specialChildren.has(p.id))
    for (let i = 0; i < pals.length; i++) for (let j = i; j < pals.length; j++) {
      const a = pals[i]; const b = pals[j]
      if (a.combiRank === null || b.combiRank === null || covered.has(key(a.id, b.id))) continue
      const rank = Math.floor((a.combiRank + b.combiRank + 1) / 2)
      const child = candidates.reduce((best, candidate) => {
        if (!best) return candidate
        const candidateDistance = Math.abs(candidate.combiRank! - rank); const bestDistance = Math.abs(best.combiRank! - rank)
        return candidateDistance < bestDistance
          || candidateDistance === bestDistance && (candidate.combiDuplicatePriority ?? 0) > (best.combiDuplicatePriority ?? 0)
          ? candidate
          : best
      }, null as PalworldPalListItem | null)
      if (child) this.add({ parentAPalId: a.id, parentAGender: null, parentBPalId: b.id, parentBGender: null, childPalId: child.id, rule: 'formula' })
    }
  }

  private add(pair: Pair) {
    for (const id of new Set([pair.parentAPalId, pair.parentBPalId])) {
      this.pairsByParent.set(id, [...(this.pairsByParent.get(id) ?? []), pair])
    }
  }

  compute(target: number, owned: Set<number>): BreedingPathResult {
    const node = (id: number, isOwned = owned.has(id)): BreedingPathNode => {
      const pal = this.pals.get(id)!
      return { species: { id, tribe: pal.tribe, name: pal.name, combiRank: pal.combiRank }, owned: isOwned, step: null }
    }

    if (owned.has(target)) {
      const ownedTarget = node(target)
      return {
        reachable: true,
        root: {
          ...node(target, false),
          step: { parentA: ownedTarget, parentB: ownedTarget, parentAGender: null, parentBGender: null, rule: 'formula' },
        },
      }
    }

    const score = new Map<number, Score>()
    const from = new Map<number, Pair>()
    const queue = [...owned]
    owned.forEach(id => score.set(id, { breeds: 0, depth: 0 }))

    while (queue.length) {
      const id = queue.shift()!
      for (const pair of this.pairsByParent.get(id) ?? []) {
        const parentA = score.get(pair.parentAPalId)
        const parentB = score.get(pair.parentBPalId)
        if (!parentA || !parentB) continue

        const next = {
          breeds: parentA.breeds + parentB.breeds + 1,
          depth: Math.max(parentA.depth, parentB.depth) + 1,
        }
        const current = score.get(pair.childPalId)
        if (current && (current.breeds < next.breeds || current.breeds === next.breeds && current.depth <= next.depth)) continue

        score.set(pair.childPalId, next)
        from.set(pair.childPalId, pair)
        queue.push(pair.childPalId)
      }
    }

    if (!score.has(target)) return { reachable: false, root: null }

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

    return { reachable: true, root: buildNode(target) }
  }
}
