import { describe, expect, it } from 'vitest'
import {
  BreedingPathEngine,
  type BreedingOwnedPal,
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

const stepsOf = (node: BreedingPathNode): { child: BreedingPathNode; parentA: BreedingPathNode; parentB: BreedingPathNode }[] =>
  node.step
    ? [
      { child: node, parentA: node.step.parentA, parentB: node.step.parentB },
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

/** Catalogue où la cible peut aussi naître d'elle-même (X + X = X), comme dans le jeu. */
const selfPairEngine = () => new BreedingPathEngine(BASE_PALS, [
  rule(CATTIVA, SOLENNE, SHROOMER),
  rule(SHROOMER, SHROOMER, SHROOMER),
])

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
    expect(speciesPairsOf(engine.compute(18, new Set([10, 30])))).not.toContain('10+30')
  })

  it('exclut un Pal `ignoreCombi` des enfants possibles par formule', () => {
    const engine = new BreedingPathEngine(
      [rankedPal(10, 'Low', 10), rankedPal(20, 'Ignored', 20, 0, true), rankedPal(30, 'High', 30)],
      [],
    )
    expect(engine.compute(20, new Set([10, 30])).reachable).toBe(false)
  })

  it('exclut du pool de formule un Pal déjà enfant d’une exception', () => {
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

describe('BreedingPathEngine — posséder la cible n’interrompt pas la recherche', () => {
  it('propose quand même un couple en mode espèces', () => {
    const result = baseEngine().compute(SHROOMER, new Set([SHROOMER, CATTIVA, SOLENNE]))
    expect(result.routes[0].root.step).not.toBeNull()
    expect(speciesPairOf(result.routes[0].root)).toBe(`${CATTIVA}+${SOLENNE}`)
  })

  it('propose quand même un couple en mode direct', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male'), own(CATTIVA, 'Male'), own(SOLENNE, 'Female'),
    ])
    expect(result.routes[0].root.step).not.toBeNull()
    expect(speciesPairOf(result.routes[0].root)).toBe(`${CATTIVA}+${SOLENNE}`)
  })

  it('propose quand même un couple en mode passifs', () => {
    const result = baseEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['P1']), own(CATTIVA, 'Male', ['P1']), own(SOLENNE, 'Female'),
    ], ['P1'])
    expect(result.routes[0].root.step).not.toBeNull()
    expect(result.routes[0].passiveCount).toBe(1)
  })

  it('propose le couple formé des Pals cibles déjà possédés, mâle et femelle avec le passif', () => {
    // Scénario réel : deux Eidrolon possédés avec Maîtrise Exceptionnelle. On veut qu'ils soient
    // proposés comme couple parent, pas qu'on s'arrête sur « tu en as déjà un ».
    const result = selfPairEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['ME'], 'dimensional_storage'),
      own(SHROOMER, 'Female', ['ME'], 'dimensional_storage'),
      own(CATTIVA, 'Male'),
      own(SOLENNE, 'Female'),
    ], ['ME'])

    expect(speciesPairOf(result.routes[0].root)).toBe(`${SHROOMER}+${SHROOMER}`)
    expect(result.routes[0].passiveCount).toBe(1)
    expect(result.routes[0].breeds).toBe(1)
    const step = result.routes[0].root.step!
    expect([step.parentA.gender, step.parentB.gender].sort()).toEqual(['Female', 'Male'])
    expect(step.parentA.owned && step.parentB.owned).toBe(true)
    expect(step.parentA.passiveSkillIds).toEqual(['ME'])
  })

  it('préfère le couple issu de la cible à un couple quelconque, à passifs et étapes égaux', () => {
    const result = selfPairEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male', ['ME']),
      own(SHROOMER, 'Female', ['ME']),
      own(CATTIVA, 'Male', ['ME']),
      own(SOLENNE, 'Female', ['ME']),
    ], ['ME'])
    expect(speciesPairOf(result.routes[0].root)).toBe(`${SHROOMER}+${SHROOMER}`)
    // L'alternative reste proposée, elle n'est simplement pas en tête.
    expect(speciesPairsOf(result)).toContain(`${CATTIVA}+${SOLENNE}`)
  })

  it('ne préfère pas un couple issu de la cible s’il perd le passif recherché', () => {
    const result = selfPairEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male'),
      own(SHROOMER, 'Female'),
      own(CATTIVA, 'Male', ['ME']),
      own(SOLENNE, 'Female'),
    ], ['ME'])
    expect(speciesPairOf(result.routes[0].root)).toBe(`${CATTIVA}+${SOLENNE}`)
    expect(result.routes[0].passiveCount).toBe(1)
  })

  it('cherche un autre mâle pour remplacer la cible quand on n’en possède qu’une femelle', () => {
    // Une seule femelle Eidrolon avec le passif, aucun mâle de la même espèce : le moteur doit
    // trouver un autre mâle capable de former un couple qui produit quand même un Eidrolon.
    const engine = new BreedingPathEngine(BASE_PALS, [rule(SHROOMER, ALT_A, SHROOMER)])
    const result = engine.compute(SHROOMER, [
      own(SHROOMER, 'Female', ['ME'], 'dimensional_storage'),
      own(ALT_A, 'Male'),
    ], ['ME'])

    expect(result.reachable).toBe(true)
    expect(speciesPairOf(result.routes[0].root)).toBe(`${SHROOMER}+${ALT_A}`)
    expect(result.routes[0].passiveCount).toBe(1)
    expect(result.routes[0].breeds).toBe(1)
  })

  it('renvoie le Pal possédé seul, en dernier recours, quand aucun couple n’existe', () => {
    const engine = new BreedingPathEngine([pal(1, 'A')], [])
    const modes: BreedingPathResult[] = [
      engine.compute(1, new Set([1])),
      engine.compute(1, [own(1, 'Male')]),
      engine.compute(1, [own(1, 'Male', ['P1'])], ['P1']),
    ]
    modes.forEach(result => {
      expect(result.reachable).toBe(true)
      expect(result.routes).toHaveLength(1)
      expect(result.routes[0].root.owned).toBe(true)
      expect(result.routes[0].root.step).toBeNull()
      expect(result.routes[0].breeds).toBe(0)
    })
  })

  it('retourne non atteignable si aucun couple n’existe et que la cible n’est pas possédée', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [])
    expect(engine.compute(2, new Set([1])).reachable).toBe(false)
    expect(engine.compute(2, [own(1, 'Male')]).reachable).toBe(false)
    expect(engine.compute(2, [own(1, 'Male', ['P1'])], ['P1']).reachable).toBe(false)
  })
})

describe('BreedingPathEngine — priorité des critères de classement', () => {
  it('préfère la route qui transmet le passif, même si elle demande plus d’étapes', () => {
    const pals = [pal(1, 'A'), pal(2, 'B'), pal(3, 'C'), pal(4, 'D'), pal(5, 'E'), pal(6, 'F'), pal(7, 'T')]
    const engine = new BreedingPathEngine(pals, [
      rule(1, 2, 7), // A + B = T : 1 croisement, sans le passif
      rule(3, 4, 5), // C + D = E : E hérite du passif de C
      rule(5, 6, 7), // E + F = T : 2 croisements, avec le passif
    ])
    const result = engine.compute(7, [
      own(1, 'Male'), own(2, 'Female'),
      own(3, 'Male', ['P1']), own(4, 'Female'), own(6, 'Female'),
    ], ['P1'])

    expect(result.routes[0].passiveCount).toBe(1)
    expect(result.routes[0].breeds).toBe(2)
    expect(speciesPairOf(result.routes[0].root)).toBe('5+6')
  })

  it('préfère le moins d’étapes à passifs égaux', () => {
    const pals = [pal(1, 'A'), pal(2, 'B'), pal(3, 'C'), pal(4, 'D'), pal(5, 'E'), pal(6, 'F'), pal(7, 'T')]
    const engine = new BreedingPathEngine(pals, [
      rule(1, 2, 7), // A + B = T : 1 croisement, avec le passif porté par A
      rule(3, 4, 5), // C + D = E
      rule(5, 6, 7), // E + F = T : 2 croisements, avec le passif porté par C
    ])
    const result = engine.compute(7, [
      own(1, 'Male', ['P1']), own(2, 'Female'),
      own(3, 'Male', ['P1']), own(4, 'Female'), own(6, 'Female'),
    ], ['P1'])

    expect(result.routes[0].passiveCount).toBe(1)
    expect(result.routes[0].breeds).toBe(1)
    expect(speciesPairOf(result.routes[0].root)).toBe('1+2')
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
})

describe('BreedingPathEngine — mode espèces (sans genre ni passif)', () => {
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
    expect(baseEngine().compute(SHROOMER, new Set([CATTIVA]))).toEqual({ reachable: false, root: null, routes: [] })
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
    baseEngine().compute(SHROOMER, new Set([CATTIVA, SOLENNE]), [], progress => stages.push(progress.stage))
    expect(stages).toContain('building-routes')
  })
})

describe('BreedingPathEngine — mode direct (genres, sans passif)', () => {
  it('croise un mâle et une femelle en une étape', () => {
    const result = baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, 'Female')])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].breeds).toBe(1)
    expect(result.routes[0].root.step?.parentA.gender).toBe('Male')
    expect(result.routes[0].root.step?.parentB.gender).toBe('Female')
  })

  it('refuse un couple de même genre', () => {
    expect(baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, 'Male')]).reachable).toBe(false)
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

  it('place en tête le couple formé de deux Pals de l’espèce cible possédés', () => {
    const result = selfPairEngine().compute(SHROOMER, [
      own(SHROOMER, 'Male'), own(SHROOMER, 'Female'),
      own(CATTIVA, 'Male'), own(SOLENNE, 'Female'),
    ])
    expect(speciesPairOf(result.routes[0].root)).toBe(`${SHROOMER}+${SHROOMER}`)
  })

  it('exclut des routes directes les Pals sans genre renseigné', () => {
    expect(baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male'), own(SOLENNE, null)]).reachable).toBe(false)
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

  it('propose la meilleure route possible quand le passif demandé n’est possédé par personne', () => {
    // Un passif que personne ne porte n'entre jamais dans le calcul : la recherche aboutit
    // quand même, simplement sans ce passif.
    const result = baseEngine().compute(SHROOMER, [
      own(CATTIVA, 'Male'),
      own(SOLENNE, 'Female'),
    ], ['PassifIntrouvable'])
    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(0)
    expect(result.routes[0].root.passiveSkillIds).toEqual([])
    expect(speciesPairOf(result.routes[0].root)).toBe(`${CATTIVA}+${SOLENNE}`)
  })

  it('n’accouple que des genres opposés même avec beaucoup de Pals d’une même espèce', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [rule(1, 1, 2)])
    const result = engine.compute(2, [
      own(1, 'Female', ['P1'], 'dimensional_storage'),
      own(1, 'Female', ['P1'], 'palbox'),
      own(1, 'Female', ['P1'], 'party'),
      own(1, 'Male', ['P1'], 'base'),
    ], ['P1'])
    expect(result.reachable).toBe(true)
    const step = result.routes[0].root.step!
    expect([step.parentA.gender, step.parentB.gender].sort()).toEqual(['Female', 'Male'])
  })

  it('déclare non atteignable une espèce requise possédée dans un seul genre', () => {
    const engine = new BreedingPathEngine([pal(1, 'A'), pal(2, 'B')], [rule(1, 1, 2)])
    const result = engine.compute(2, [
      own(1, 'Female', ['P1'], 'dimensional_storage'),
      own(1, 'Female', ['P1'], 'palbox'),
    ], ['P1'])
    expect(result.reachable).toBe(false)
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
    expect(baseEngine().compute(SHROOMER, [own(CATTIVA, 'Male', ['P1'])], ['P1']))
      .toEqual({ reachable: false, root: null, routes: [] })
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
        run().routes.forEach(route => {
          expect(route.root.species.id).toBe(SHROOMER)
          expect(route.root.owned).toBe(false)
        })
      })

      it('n’utilise que des Pals réellement possédés comme feuilles', () => {
        run().routes.forEach(route => {
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
            const parents = [step.parentA.species.id, step.parentB.species.id].sort((a, b) => a - b).join('+')
            expect(allowed).toContain(`${parents}=${step.child.species.id}`)
          })
        })
      })

      it('n’accouple jamais deux Pals du même genre', () => {
        run().routes.forEach(route => {
          stepsOf(route.root).forEach(step => {
            if (step.parentA.gender === null || step.parentB.gender === null) return
            expect(step.parentA.gender).not.toBe(step.parentB.gender)
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

    const startedAt = Date.now()
    const result = engine.compute(Math.floor(size / 2), owned, ['P1', 'P2', 'P3'])
    const elapsed = Date.now() - startedAt

    expect(result.reachable).toBe(true)
    expect(result.routes[0].passiveCount).toBe(3)
    // Seuil large : il ne sert qu'à détecter une explosion algorithmique, pas à mesurer finement.
    expect(elapsed).toBeLessThan(10_000)
  })
})
