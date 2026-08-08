import { describe, expect, it } from 'vitest'
import {
  BreedingPathEngine,
  type BreedingOwnedPal,
  type BreedingPathOptions,
  type BreedingPathPalDefinition,
  type BreedingPathProgress,
  type BreedingRuleData,
} from './BreedingPathEngine'
import type { BreedingPathNode, BreedingPathResult } from '../types/breeding.types'

/**
 * Un Pal sans `combiRank` ne participe à aucune combinaison par formule : les catalogues
 * construits avec ce helper n'ont donc que les couples explicitement déclarés en règles,
 * ce qui rend les scénarios totalement déterministes.
 */
const pal = (id: number, name: string): BreedingPathPalDefinition => ({
  id,
  tribe: name,
  name,
  combiRank: null,
  combiDuplicatePriority: null,
  ignoreCombi: false,
})

const rankedPal = (
  id: number,
  name: string,
  combiRank: number,
  combiDuplicatePriority = 0,
  ignoreCombi = false,
): BreedingPathPalDefinition => ({ id, tribe: name, name, combiRank, combiDuplicatePriority, ignoreCombi })

const rule = (
  parentAPalId: number,
  parentBPalId: number,
  childPalId: number,
  parentAGender: 'Male' | 'Female' | null = null,
  parentBGender: 'Male' | 'Female' | null = null,
): BreedingRuleData => ({ parentAPalId, parentAGender, parentBPalId, parentBGender, childPalId })

const own = (
  speciesId: number,
  gender: 'Male' | 'Female' | null = null,
  passiveSkillIds: string[] = [],
  storageLocation: BreedingOwnedPal['storageLocation'] = 'party',
): BreedingOwnedPal => ({ speciesId, passiveSkillIds, gender, storageLocation })

/** Couple d'espèces d'une route, normalisé pour comparer sans dépendre de l'ordre des parents. */
const speciesPairOf = (node: BreedingPathNode) => {
  const step = node.step!
  return [step.parentA.species.id, step.parentB.species.id].sort((a, b) => a - b).join('+')
}

const speciesPairsOf = (result: BreedingPathResult) => result.routes.map(route => speciesPairOf(route.root))

const leavesOf = (node: BreedingPathNode): BreedingPathNode[] => node.step
  ? [...leavesOf(node.step.parentA), ...leavesOf(node.step.parentB)]
  : [node]

const stepsOf = (node: BreedingPathNode): { childId: number; parentAId: number; parentBId: number }[] => node.step
  ? [
    { childId: node.species.id, parentAId: node.step.parentA.species.id, parentBId: node.step.parentB.species.id },
    ...stepsOf(node.step.parentA),
    ...stepsOf(node.step.parentB),
  ]
  : []

const CATTIVA = 1
const SOLENNE = 2
const SHROOMER = 3
const ALT_A = 4
const ALT_B = 5

const BASE_PALS = [
  pal(CATTIVA, 'Cattiva'),
  pal(SOLENNE, 'Solenne'),
  pal(SHROOMER, 'Shroomer'),
  pal(ALT_A, 'AltA'),
  pal(ALT_B, 'AltB'),
]

/** Deux couples distincts mènent à Shroomer : de quoi vérifier les routes alternatives. */
const BASE_RULES = [rule(CATTIVA, SOLENNE, SHROOMER), rule(ALT_A, ALT_B, SHROOMER)]

const baseEngine = () => new BreedingPathEngine(BASE_PALS, BASE_RULES)

describe('BreedingPathEngine — catalogue et construction des couples', () => {
  it('refuse un catalogue où un Pal a un rang sans priorité de doublon', () => {
    const incomplete: BreedingPathPalDefinition = {
      id: 1, tribe: 'A', name: 'A', combiRank: 10, combiDuplicatePriority: null, ignoreCombi: false,
    }
    expect(() => new BreedingPathEngine([incomplete], [])).toThrow('Le catalogue d’élevage est incomplet.')
  })

  it('accepte un Pal sans rang même sans priorité de doublon', () => {
    expect(() => new BreedingPathEngine([pal(1, 'A')], [])).not.toThrow()
  })

  it('ne génère aucun couple par formule quand aucun Pal n’a de rang', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [])
    expect(engine.compute(2, new Set([1])).reachable).toBe(false)
  })

  it('résout l’enfant d’une formule par le rang moyen des parents', () => {
    // rang = floor((10 + 30 + 1) / 2) = 20 → le Pal de rang 20.
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(20, 'Mid', 20), rankedPal(30, 'High', 30)],
      [],
    )
    const result = engine.compute(20, new Set([10, 30]))
    expect(result.reachable).toBe(true)
    expect(speciesPairsOf(result)).toContain('10+30')
    expect(result.routes[0].root.step?.rule).toBe('formula')
  })

  it('départage deux rangs équidistants par la priorité de doublon la plus élevée', () => {
    // rang visé par 10 + 30 = floor((10 + 30 + 1) / 2) = 20, à égale distance de 18 et de 22.
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(18, 'NearLow', 18, 0), rankedPal(22, 'NearHigh', 22, 9), rankedPal(30, 'High', 30)],
      [],
    )
    const winner = engine.compute(22, new Set([10, 30]))
    expect(winner.routes[0].breeds).toBe(1)
    expect(speciesPairOf(winner.routes[0].root)).toBe('10+30')

    // Le rang perdant reste accessible autrement, mais jamais directement depuis 10 + 30.
    const loser = engine.compute(18, new Set([10, 30]))
    expect(speciesPairsOf(loser)).not.toContain('10+30')
  })

  it('exclut un Pal `ignoreCombi` des enfants possibles par formule', () => {
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(20, 'Ignored', 20, 0, true), rankedPal(30, 'High', 30)],
      [],
    )
    expect(engine.compute(20, new Set([10, 30])).reachable).toBe(false)
  })

  it('exclut du pool de formule un Pal déjà enfant d’une exception', () => {
    // Mid est enfant d'une exception : la formule 10+30 ne doit plus le produire.
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(20, 'Mid', 20), rankedPal(30, 'High', 30)],
      [rule(10, 10, 20)],
    )
    const result = engine.compute(20, new Set([10, 30]))
    expect(speciesPairsOf(result)).not.toContain('10+30')
    expect(speciesPairsOf(result)).toContain('10+10')
  })

  it('donne la priorité à l’exception sur la formule pour un même couple', () => {
    // Sans l'exception, 10 + 20 donnerait le rang 15 → le Pal de rang 10, jamais celui de rang 30.
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(20, 'Mid', 20), rankedPal(30, 'High', 30)],
      [rule(10, 20, 30)],
    )
    const result = engine.compute(30, new Set([10, 20]))
    expect(result.routes[0].breeds).toBe(1)
    expect(speciesPairOf(result.routes[0].root)).toBe('10+20')
    expect(result.routes[0].root.step?.rule).toBe('exception')
  })
})

describe('BreedingPathEngine — garde-fous d’entrée', () => {
  it('retourne non atteignable quand la cible est absente du catalogue', () => {
    expect(baseEngine().compute(999, [own(CATTIVA, 'Male')])).toEqual({ reachable: false, root: null, routes: [] })
  })

  it('ignore une espèce possédée absente du catalogue au lieu de planter', () => {
    const engine = baseEngine()
    // Le catalogue et les Pals possédés viennent de deux sources : une désynchro ne doit pas crasher.
    expect(() => engine.compute(SHROOMER, new Set([999]))).not.toThrow()
    expect(() => engine.compute(SHROOMER, [own(999, 'Male')])).not.toThrow()
    expect(() => engine.compute(SHROOMER, [own(999, 'Male', ['P1'])], ['P1'])).not.toThrow()
    expect(engine.compute(SHROOMER, new Set([999])).reachable).toBe(false)
  })

  it('retourne non atteignable sans aucun Pal possédé', () => {
    expect(baseEngine().compute(SHROOMER, []).reachable).toBe(false)
    expect(baseEngine().compute(SHROOMER, new Set<number>()).reachable).toBe(false)
    expect(baseEngine().compute(SHROOMER, [], ['P1']).reachable).toBe(false)
  })

  it('déduplique les passifs demandés en double', () => {
    const engine = baseEngine()
    const owned = [own(CATTIVA, 'Male', ['P1']), own(SOLENNE, 'Female')]
    const single = engine.compute(SHROOMER, owned, ['P1'])
    const duplicated = engine.compute(SHROOMER, owned, ['P1', 'P1', 'P1'])
    expect(duplicated.routes[0].passiveCount).toBe(single.routes[0].passiveCount)
    expect(duplicated.routes[0].root.passiveSkillIds).toEqual(['P1'])
  })

  it('accepte un Set d’identifiants comme liste de Pals possédés', () => {
    const result = baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE]))
    expect(result.reachable).toBe(true)
    expect(speciesPairsOf(result)).toContain(`${CATTIVA}+${SOLENNE}`)
  })
})

describe('BreedingPathEngine — mode espèces (sans genre ni passif)', () => {
  it('propose directement la cible quand elle est déjà possédée', () => {
    const result = baseEngine().compute(SHROOMER, new Set([SHROOMER, CATTIVA, SOLENNE]))
    expect(result.routes).toHaveLength(1)
    expect(result.routes[0].id).toBe(`owned-target:${SHROOMER}`)
    expect(result.routes[0].root.step?.parentA.owned).toBe(true)
  })

  it('trouve un croisement en une étape', () => {
    const result = baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE]))
    expect(result.reachable).toBe(true)
    expect(result.routes[0].breeds).toBe(1)
    expect(speciesPairOf(result.routes[0].root)).toBe(`${CATTIVA}+${SOLENNE}`)
  })

  it('enchaîne plusieurs croisements et compte les étapes', () => {
    const engine = new BreedingPathEngine(
      [pal(1, 'A'), pal(2, 'B'), pal(3, 'C'), pal(4, 'D')],
      [rule(1, 2, 3), rule(3, 2, 4)],
    )
    const result = engine.compute(4, new Set([1, 2]))
    expect(result.reachable).toBe(true)
    expect(result.routes[0].breeds).toBe(2)
    expect(stepsOf(result.routes[0].root)).toHaveLength(2)
  })

  it('gère un auto-croisement X + X = Y', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [rule(1, 1, 2)])
    const result = engine.compute(2, new Set([1]))
    expect(result.reachable).toBe(true)
    expect(speciesPairOf(result.routes[0].root)).toBe('1+1')
  })

  it('retourne non atteignable quand aucune règle ne mène à la cible', () => {
    const result = baseEngine().compute(SHROOMER, new Set([CATTIVA]))
    expect(result).toEqual({ reachable: false, root: null, routes: [] })
  })

  it('propose plusieurs couples distincts en routes alternatives', () => {
    const result = baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE, ALT_A, ALT_B]))
    expect(speciesPairsOf(result).sort()).toEqual([`${CATTIVA}+${SOLENNE}`, `${ALT_A}+${ALT_B}`].sort())
  })

  it('plafonne à 5 routes', () => {
    const pals = Array.from({ length: 13 }, (_, i) => pal(i + 1, `Pal${i + 1}`))
    const rules = Array.from({ length: 6 }, (_, i) => rule(i * 2 + 1, i * 2 + 2, 13))
    const engine = new BreedingPathEngine(pals, rules)
    const result = engine.compute(13, new Set(Array.from({ length: 12 }, (_, i) => i + 1)))
    expect(result.routes).toHaveLength(5)
  })

  it('termine et produit un arbre fini malgré des règles cycliques', () => {
    const engine = new BreedingPathEngine(
      [pal(1, 'A'), pal(2, 'B'), pal(3, 'C')],
      [rule(1, 2, 3), rule(3, 2, 1), rule(1, 3, 2)],
    )
    const result = engine.compute(3, new Set([1, 2]))
    expect(result.reachable).toBe(true)
    expect(stepsOf(result.routes[0].root).length).toBeLessThan(10)
  })

  it('notifie la progression jusqu’à la construction des routes', () => {
    const stages: BreedingPathProgress['stage'][] = []
    baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE]), [], {}, progress => stages.push(progress.stage))
    expect(stages).toContain('building-routes')
  })
})

describe('BreedingPathEngine — mode direct (genres, sans passif)', () => {
  it('propose directement la cible quand elle est déjà possédée', () => {
    const result = baseEngine().compute(SHROOMER, [own(SHROOMER, 'Male'), own(CATTIVA, 'Male'), own(SOLENNE, 'Female')])
    expect(result.routes).toHaveLength(1)
    expect(result.routes[0].id).toBe(`owned-target:${SHROOMER}`)
  })

  it('croise un mâle et une femelle en une étape', () => {
    const result = baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, 'Female')])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].breeds).toBe(1)
    expect(result.routes[0].root.step?.parentA.gender).toBe('Male')
    expect(result.routes[0].root.step?.parentB.gender).toBe('Female')
  })

  it('refuse un couple de même genre', () => {
    const result = baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, 'Male')])
    expect(result.reachable).toBe(false)
  })

  it('respecte la contrainte de genre portée par une exception', () => {
    const engine = new BreedingPathEngine(BASE_PALS, [rule(CATTIVA, SOLENNE, SHROOMER, 'Male', 'Female')])
    expect(engine.compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, 'Female')]).reachable).toBe(true)
    expect(engine.compute(SHROOMER, [own(CATTIVA, 'Female'), own(SOLENNE, 'Male')]).reachable).toBe(false)
  })

  it('ne propose pas deux fois le même couple d’espèces', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male', [], 'party'),
      own(CATTIVA, 'Male', [], 'base'),
      own(CATTIVA, 'Female', [], 'palbox'),
      own(SOLENNE, 'Female', [], 'party'),
      own(SOLENNE, 'Male', [], 'base'),
    ])
    const pairs = speciesPairsOf(result)
    expect(new Set(pairs).size).toBe(pairs.length)
    expect(pairs).toEqual([`${CATTIVA}+${SOLENNE}`])
  })

  it('propose le Pal possédé plutôt qu’un croisement cible + cible, priorisation comprise', () => {
    // Un couple cible + cible suppose de posséder la cible : dans ce cas elle est proposée
    // telle quelle, sans demander de la ré-élever.
    const engine = new BreedingPathEngine(BASE_PALS, [
      rule(CATTIVA, SOLENNE, SHROOMER),
      rule(SHROOMER, SHROOMER, SHROOMER),
    ])
    const owned = [own(SHROOMER, 'Male'), own(SHROOMER, 'Female'), own(CATTIVA, 'Male'), own(SOLENNE, 'Female')]
    expect(engine.compute(SHROOMER, owned, [], { prioritizeTargetSpecies: true }).routes[0].id)
      .toBe(`owned-target:${SHROOMER}`)
    expect(engine.compute(SHROOMER, owned, [], { prioritizeTargetSpecies: false }).routes[0].id)
      .toBe(`owned-target:${SHROOMER}`)
  })

  it('exclut des routes directes les Pals sans genre renseigné', () => {
    const result = baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, null)])
    expect(result.reachable).toBe(false)
  })

  it('bascule sur une recherche multi-étapes quand aucun croisement direct n’existe', () => {
    const engine = new BreedingPathEngine(
      [pal(1, 'A'), pal(2, 'B'), pal(3, 'C'), pal(4, 'D')],
      [rule(1, 2, 3), rule(3, 2, 4)],
    )
    const result = engine.compute(4, [own(1, 'Male'), own(2, 'Female')])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].breeds).toBe(2)
    expect(stepsOf(result.routes[0].root)).toHaveLength(2)
  })

  it('plafonne à 5 routes', () => {
    const pals = Array.from({ length: 13 }, (_, i) => pal(i + 1, `Pal${i + 1}`))
    const rules = Array.from({ length: 6 }, (_, i) => rule(i * 2 + 1, i * 2 + 2, 13))
    const engine = new BreedingPathEngine(pals, rules)
    const owned = Array.from({ length: 12 }, (_, i) => own(i + 1, i % 2 === 0 ? 'Male' : 'Female'))
    expect(engine.compute(13, owned).routes).toHaveLength(5)
  })
})

describe('BreedingPathEngine — mode passifs', () => {
  it('propose le Pal cible possédé qui couvre déjà tous les passifs demandés', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['Exceptionnel']),
      own(CATTIVA, 'Male'),
      own(SOLENNE, 'Female'),
    ], ['Exceptionnel'])
    expect(result.routes).toHaveLength(1)
    expect(result.routes[0].id).toMatch(/^owned-target:/)
    expect(result.routes[0].passiveCount).toBe(1)
    expect(result.routes[0].root.step?.parentA.owned).toBe(true)
    expect(result.routes[0].root.step?.parentA.passiveSkillIds).toEqual(['Exceptionnel'])
  })

  it('propose le Pal cible possédé même sans genre renseigné', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, null, ['Exceptionnel']),
      own(CATTIVA, null),
      own(SOLENNE, null),
    ], ['Exceptionnel'])
    expect(result.routes[0].id).toMatch(/^owned-target:/)
  })

  it('propose le Pal cible possédé même en couverture partielle si rien ne fait mieux', () => {
    // Aucun parent ne porte P2 : le Shroomer possédé (P1) reste le meilleur résultat possible.
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['P1']),
      own(CATTIVA, 'Male'),
      own(SOLENNE, 'Female'),
    ], ['P1', 'P2'])
    expect(result.routes[0].id).toMatch(/^owned-target:/)
    expect(result.routes[0].passiveCount).toBe(1)
  })

  it('préfère une route d’élevage quand elle apporte plus de passifs que le Pal possédé', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['P1']),
      own(CATTIVA, 'Male', ['P1']),
      own(SOLENNE, 'Female', ['P2']),
    ], ['P1', 'P2'])
    expect(result.routes[0].id).not.toMatch(/^owned-target:/)
    expect(result.routes[0].passiveCount).toBe(2)
    expect(result.routes[0].root.passiveSkillIds.sort()).toEqual(['P1', 'P2'])
  })

  it('cumule les passifs des deux parents chez l’enfant', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male', ['P1']),
      own(SOLENNE, 'Female', ['P2']),
    ], ['P1', 'P2'])
    expect(result.routes[0].root.passiveSkillIds.sort()).toEqual(['P1', 'P2'])
    expect(result.routes[0].passiveCount).toBe(2)
  })

  it('combine les passifs via un auto-croisement de la même espèce (X + X = X)', () => {
    // Technique de base du jeu : croiser deux Pals de la même espèce pour cumuler leurs passifs.
    const engine = new BreedingPathEngine([pal(1, 'A')], [rule(1, 1, 1)])
    const result = engine.compute(1, [
      own(1, 'Male', ['P1'], 'party'),
      own(1, 'Female', ['P2'], 'base'),
    ], ['P1', 'P2'])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(2)
    expect(result.routes[0].root.passiveSkillIds.sort()).toEqual(['P1', 'P2'])
    expect(leavesOf(result.routes[0].root).every(leaf => leaf.owned)).toBe(true)
  })

  it('combine les passifs via un auto-croisement produisant une autre espèce (X + X = Y)', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [rule(1, 1, 2)])
    const result = engine.compute(2, [
      own(1, 'Male', ['P1'], 'party'),
      own(1, 'Female', ['P2'], 'base'),
    ], ['P1', 'P2'])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(2)
    expect(speciesPairOf(result.routes[0].root)).toBe('1+1')
  })

  it('accumule les passifs sur plusieurs générations', () => {
    const engine = new BreedingPathEngine(
      [pal(1, 'A'), pal(2, 'B'), pal(3, 'C'), pal(4, 'D')],
      [rule(1, 2, 3), rule(3, 4, 3)],
    )
    const result = engine.compute(3, [
      own(1, 'Male', ['P1']),
      own(2, 'Female', ['P2']),
      own(4, 'Female', ['P3']),
    ], ['P1', 'P2', 'P3'])
    expect(result.routes[0].passiveCount).toBe(3)
    expect(result.routes[0].breeds).toBe(2)
    expect(stepsOf(result.routes[0].root)).toHaveLength(2)
  })

  it('ignore les passifs qui ne sont pas demandés', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male', ['P1', 'HorsSujet']),
      own(SOLENNE, 'Female', ['AutreHorsSujet']),
    ], ['P1'])
    expect(result.routes[0].root.passiveSkillIds).toEqual(['P1'])
    expect(result.routes[0].passiveCount).toBe(1)
  })

  it('respecte les genres en mode passifs', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male', ['P1']),
      own(SOLENNE, 'Male', ['P2']),
    ], ['P1', 'P2'])
    expect(result.reachable).toBe(false)
  })

  it('respecte la contrainte de genre d’une exception en mode passifs', () => {
    const engine = new BreedingPathEngine(BASE_PALS, [rule(CATTIVA, SOLENNE, SHROOMER, 'Male', 'Female')])
    const inverted = engine.compute(SHROOMER, [
      own(CATTIVA, 'Female', ['P1']),
      own(SOLENNE, 'Male', ['P2']),
    ], ['P1', 'P2'])
    expect(inverted.reachable).toBe(false)
  })

  it('retourne non atteignable quand la cible n’est pas productible', () => {
    const result = baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male', ['P1'])], ['P1'])
    expect(result).toEqual({ reachable: false, root: null, routes: [] })
  })

  it('ne propose pas deux fois le même couple d’espèces dans les alternatives', () => {
    const result = baseEngine().compute(SHROOMER, [
      // Plusieurs individus par espèce : ils ne doivent pas produire de routes en double.
      own(CATTIVA, 'Male', [], 'party'),
      own(CATTIVA, 'Male', [], 'base'),
      own(CATTIVA, 'Female', [], 'palbox'),
      own(SOLENNE, 'Female', [], 'party'),
      own(SOLENNE, 'Male', [], 'base'),
      own(ALT_A, 'Male', [], 'party'),
      own(ALT_B, 'Female', [], 'party'),
    ], ['JamaisPossede'])
    const pairs = speciesPairsOf(result)
    expect(new Set(pairs).size).toBe(pairs.length)
    expect(pairs.sort()).toEqual([`${CATTIVA}+${SOLENNE}`, `${ALT_A}+${ALT_B}`].sort())
  })

  it('privilégie un croisement issu de la cible quand la priorisation est activée', () => {
    // Les deux Shroomer possédés ne portent qu'un passif chacun : aucun ne suffit, deux routes
    // d'élevage à 2 passifs s'affrontent, et la priorisation doit départager en faveur de la cible.
    const engine = new BreedingPathEngine(BASE_PALS, [
      rule(CATTIVA, SOLENNE, SHROOMER),
      rule(SHROOMER, SHROOMER, SHROOMER),
    ])
    const owned = [
      own(SHROOMER, 'Male', ['P1'], 'party'),
      own(SHROOMER, 'Female', ['P2'], 'base'),
      own(CATTIVA, 'Male', ['P1']),
      own(SOLENNE, 'Female', ['P2']),
    ]

    const prioritized = engine.compute(SHROOMER, owned, ['P1', 'P2'], { prioritizeTargetSpecies: true })
    expect(prioritized.routes[0].passiveCount).toBe(2)
    expect(speciesPairOf(prioritized.routes[0].root)).toBe(`${SHROOMER}+${SHROOMER}`)

    const neutral = engine.compute(SHROOMER, owned, ['P1', 'P2'], {})
    expect(speciesPairsOf(neutral)).toContain(`${CATTIVA}+${SOLENNE}`)
  })

  it('trie les routes par nombre de passifs décroissant', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male', ['P1']),
      own(SOLENNE, 'Female', ['P2']),
      own(ALT_A, 'Male'),
      own(ALT_B, 'Female'),
    ], ['P1', 'P2'])
    const counts = result.routes.map(route => route.passiveCount)
    expect(counts).toEqual([...counts].sort((a, b) => b - a))
    expect(counts[0]).toBe(2)
  })

  it('plafonne à 5 routes', () => {
    const pals = Array.from({ length: 13 }, (_, i) => pal(i + 1, `Pal${i + 1}`))
    const rules = Array.from({ length: 6 }, (_, i) => rule(i * 2 + 1, i * 2 + 2, 13))
    const engine = new BreedingPathEngine(pals, rules)
    const owned = Array.from({ length: 12 }, (_, i) => own(i + 1, i % 2 === 0 ? 'Male' : 'Female', ['P1']))
    expect(engine.compute(13, owned, ['P1']).routes).toHaveLength(5)
  })

  it('notifie la progression jusqu’à la construction des routes', () => {
    const stages: BreedingPathProgress['stage'][] = []
    baseEngine().compute(
      SHROOMER,
      [own(CATTIVA, 'Male', ['P1']), own(SOLENNE, 'Female')],
      ['P1'],
      {},
      progress => stages.push(progress.stage),
    )
    expect(stages).toContain('building-routes')
  })

  it('ne porte aucun passif quand les Pals possédés sont fournis en Set d’espèces', () => {
    // Un Set ne transporte ni genre ni passif : la cible reste atteignable, sans passif.
    const result = baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE]), ['P1'])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(0)
  })
})

describe('BreedingPathEngine — invariants des routes renvoyées', () => {
  const scenarios: { name: string; run: () => BreedingPathResult }[] = [
    {
      name: 'mode espèces',
      run: () => baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE, ALT_A, ALT_B])),
    },
    {
      name: 'mode direct',
      run: () => baseEngine().compute(SHROOMER, [
        own(CATTIVA, 'Male'), own(SOLENNE, 'Female'), own(ALT_A, 'Male'), own(ALT_B, 'Female'),
      ]),
    },
    {
      name: 'mode passifs',
      run: () => baseEngine().compute(SHROOMER, [
        own(CATTIVA, 'Male', ['P1']), own(SOLENNE, 'Female', ['P2']),
        own(ALT_A, 'Male', ['P1']), own(ALT_B, 'Female'),
      ], ['P1', 'P2']),
    },
  ]

  scenarios.forEach(({ name, run }) => {
    describe(name, () => {
      it('renvoie une racine correspondant à la cible et non marquée comme possédée', () => {
        const result = run()
        result.routes.forEach(route => {
          expect(route.root.species.id).toBe(SHROOMER)
          expect(route.root.owned).toBe(false)
        })
      })

      it('n’utilise que des Pals réellement possédés comme feuilles', () => {
        const result = run()
        result.routes.forEach(route => {
          leavesOf(route.root).forEach(leaf => {
            expect(leaf.owned).toBe(true)
            expect([CATTIVA, SOLENNE, ALT_A, ALT_B]).toContain(leaf.species.id)
          })
        })
      })

      it('n’enchaîne que des croisements autorisés par les règles', () => {
        const allowed = new Set(BASE_RULES.map(r => `${[r.parentAPalId, r.parentBPalId].sort((a, b) => a - b).join('+')}=${r.childPalId}`))
        run().routes.forEach(route => {
          stepsOf(route.root).forEach(step => {
            const signature = `${[step.parentAId, step.parentBId].sort((a, b) => a - b).join('+')}=${step.childId}`
            expect(allowed).toContain(signature)
          })
        })
      })

      it('expose des identifiants de route uniques', () => {
        const ids = run().routes.map(route => route.id)
        expect(new Set(ids).size).toBe(ids.length)
      })

      it('fait correspondre la racine à la première route', () => {
        const result = run()
        expect(result.root).toBe(result.routes[0].root)
      })
    })
  })
})

describe('BreedingPathEngine — garde-fou de performance', () => {
  it('traite un catalogue réaliste avec plusieurs passifs en un temps raisonnable', () => {
    const size = 120
    const pals = Array.from({ length: size }, (_, i) => rankedPal(i + 1, `Pal${i + 1}`, i + 1))
    const engine = new BreedingPathEngine(pals, [])
    const owned: BreedingOwnedPal[] = Array.from({ length: 40 }, (_, i) => own(
      (i * 7) % size + 1,
      i % 2 === 0 ? 'Male' : 'Female',
      [`P${(i % 3) + 1}`],
      'palbox',
    ))
    const options: BreedingPathOptions = { prioritizeTargetSpecies: true }

    const startedAt = Date.now()
    const result = engine.compute(Math.floor(size / 2), owned, ['P1', 'P2', 'P3'], options)
    const elapsed = Date.now() - startedAt

    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(3)
    // Seuil large : il ne sert qu'à détecter une explosion algorithmique, pas à mesurer finement.
    expect(elapsed).toBeLessThan(10_000)
  })
})
