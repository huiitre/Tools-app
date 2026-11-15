import { describe, expect, it } from 'vitest'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import {
  baseIngredients,
  bestCraftCost,
  buyCost,
  craftCostRemaining,
  craftCostTotal,
  isComplete,
  mixedCost,
  type PriceOf,
} from './workshopCost'

const item = (id: number, hasRecipe = false) => ({ id, name: `#${id}`, hasRecipe }) as unknown as Item

const ing = (
  id: number,
  itemId: number,
  quantityRequired: number,
  quantityObtained = 0,
  parentIngredientId?: number,
): WorkshopItemIngredient => ({
  id,
  workshopItemId: 1,
  item: item(itemId),
  parentIngredientId,
  quantityRequired,
  quantityObtained,
})

const workshopItem = (
  itemId: number,
  ingredients: WorkshopItemIngredient[],
  quantity = 1,
  hasRecipe = true,
): WorkshopItem => ({ id: 1, workshopId: 1, item: item(itemId, hasRecipe), quantity, ingredients })

const prices = (map: Record<number, number>): PriceOf => (id) => map[id] ?? 0

//* L'Amulette de Viti Glourson de l'exemple : achat 4 500 000, craft 4 510 000.
const AMULETTE = 1, HAMATUM = 10, ARDONITE = 11, AUTRE = 12, SOUS_A = 20, SOUS_B = 21
const PRICES = {
  [AMULETTE]: 4_500_000,
  [HAMATUM]: 200_000,
  [ARDONITE]: 500_000,
  [AUTRE]: 3_810_000,
  [SOUS_A]: 100_000,
  [SOUS_B]: 50_000,
}

//* Données conformes à l'API : choisir de crafter un ingrédient le passe à 1/1
//* (CraftIngredientUseCase), et ses sous-ingrédients sont ajoutés à 0.
const amulette = (
  obtained: { hamatum?: number; sousA?: number; sousB?: number } = {},
  ardoniteCraftee = false,
) => {
  const ingredients = [
    ing(1, HAMATUM, 1, obtained.hamatum ?? 0),
    ing(2, ARDONITE, 1, ardoniteCraftee ? 1 : 0),
    ing(3, AUTRE, 1),
  ]
  if (ardoniteCraftee) {
    //* Ardonite craftée : 2 × 100 000 + 2 × 50 000 = 300 000 au lieu de 500 000.
    ingredients.push(ing(4, SOUS_A, 2, obtained.sousA ?? 0, 2), ing(5, SOUS_B, 2, obtained.sousB ?? 0, 2))
  }
  return workshopItem(AMULETTE, ingredients)
}

describe('baseIngredients', () => {
  it('garde les feuilles de l\'arbre, à toutes les profondeurs, et écarte les ingrédients craftés', () => {
    const ingredients = [
      ing(1, 10, 1),
      ing(2, 11, 1),          //* crafté
      ing(3, 20, 2, 0, 2),    //* crafté, sous 2
      ing(4, 21, 2, 0, 2),    //* feuille, sous 2
      ing(5, 30, 4, 0, 3),    //* feuille, sous 3
    ]
    expect(baseIngredients(ingredients).map(i => i.id)).toEqual([1, 4, 5])
  })

  it('renvoie tout quand rien n\'est crafté', () => {
    const ingredients = [ing(1, 10, 1), ing(2, 11, 3)]
    expect(baseIngredients(ingredients)).toEqual(ingredients)
  })
})

describe('craftCostTotal', () => {
  it('additionne les ressources au prix plein, sans tenir compte de ce qui est obtenu', () => {
    expect(craftCostTotal(amulette({ hamatum: 1 }).ingredients, prices(PRICES))).toBe(4_510_000)
  })

  it('compte un ingrédient crafté par ses sous-ingrédients, jamais en plus de son prix d\'achat', () => {
    //* Ancien calcul : 4 510 000 + 300 000 = 4 810 000 (Ardonite comptée deux fois).
    expect(craftCostTotal(amulette({}, true).ingredients, prices(PRICES))).toBe(4_310_000)
  })

  it('monte quand on crafte un ingrédient plus cher à crafter qu\'à acheter', () => {
    const cher = prices({ ...PRICES, [SOUS_A]: 250_000 })
    //* Ardonite craftée : 2 × 250 000 + 2 × 50 000 = 600 000, soit 100 000 de plus.
    expect(craftCostTotal(amulette({}, true).ingredients, cher)).toBe(4_610_000)
  })

  it('descend sur plusieurs niveaux de craft', () => {
    const ingredients = [
      ing(1, 10, 1),
      ing(2, 11, 1),
      ing(3, 20, 2, 0, 2),
      ing(4, 21, 4, 0, 3),
    ]
    const p = prices({ 10: 5, 11: 1_000, 20: 1_000, 21: 7 })
    expect(craftCostTotal(ingredients, p)).toBe(5 + 4 * 7)
  })
})

describe('craftCostRemaining', () => {
  it('retire ce qui est obtenu', () => {
    expect(craftCostRemaining(amulette({ hamatum: 1 }).ingredients, prices(PRICES))).toBe(4_310_000)
  })

  it('ne descend jamais sous zéro pour une ressource obtenue en trop', () => {
    const ingredients = [ing(1, 10, 2, 5), ing(2, 11, 1)]
    expect(craftCostRemaining(ingredients, prices({ 10: 100, 11: 30 }))).toBe(30)
  })

  it('ignore la quantité obtenue d\'un ingrédient crafté : seules ses ressources comptent', () => {
    const ingredients = [ing(1, 10, 1, 1), ing(2, 20, 3, 1, 1)]
    expect(craftCostRemaining(ingredients, prices({ 10: 1_000, 20: 10 }))).toBe(20)
  })

  it('vaut 0 quand tout est obtenu', () => {
    const ingredients = [ing(1, 10, 2, 2), ing(2, 11, 1, 1)]
    expect(craftCostRemaining(ingredients, prices({ 10: 100, 11: 30 }))).toBe(0)
  })
})

describe('isComplete (prix d\'achat ajusté)', () => {
  //* Ancienne règle, gardée ici pour vérifier qu'elle ne change pas quand rien n'est crafté.
  const legacyIsComplete = (ingredients: WorkshopItemIngredient[]) => ingredients
    .filter(i => !i.parentIngredientId && !ingredients.some(sub => sub.parentIngredientId === i.id))
    .every(i => i.quantityObtained >= i.quantityRequired)

  it('ressources directes toutes obtenues : terminé', () => {
    expect(isComplete([ing(1, 10, 2, 2), ing(2, 11, 1, 3)])).toBe(true)
  })

  it('une ressource directe manquante : pas terminé', () => {
    expect(isComplete([ing(1, 10, 2, 2), ing(2, 11, 1, 0)])).toBe(false)
  })

  it('sans ingrédient : terminé, comme avant', () => {
    expect(isComplete([])).toBe(true)
    expect(legacyIsComplete([])).toBe(true)
  })

  it('Ardonite craftée (1/1) dont les minerais manquent : pas terminé (l\'ancienne règle disait terminé)', () => {
    const ingredients = [ing(1, HAMATUM, 1, 1), ing(2, ARDONITE, 1, 1), ing(3, AUTRE, 1, 1), ing(4, SOUS_A, 2, 0, 2), ing(5, SOUS_B, 2, 2, 2)]
    expect(legacyIsComplete(ingredients)).toBe(true)
    expect(isComplete(ingredients)).toBe(false)
  })

  it('Ardonite craftée (1/1) dont les minerais sont obtenus : terminé', () => {
    const ingredients = [ing(1, HAMATUM, 1, 1), ing(2, ARDONITE, 1, 1), ing(3, AUTRE, 1, 1), ing(4, SOUS_A, 2, 2, 2), ing(5, SOUS_B, 2, 2, 2)]
    expect(isComplete(ingredients)).toBe(true)
    expect(legacyIsComplete(ingredients)).toBe(true)
  })

  it('identique à l\'ancienne règle sur 5 000 objets sans ingrédient crafté', () => {
    let seed = 7
    const rand = () => (seed = (seed * 1_103_515_245 + 12_345) % 2_147_483_648) / 2_147_483_648
    for (let n = 0; n < 5_000; n++) {
      const ingredients = Array.from({ length: Math.floor(rand() * 8) }, (_, i) => {
        const required = 1 + Math.floor(rand() * 10)
        return ing(i + 1, 100 + i, required, Math.floor(rand() * (required + 2)))
      })
      expect(isComplete(ingredients)).toBe(legacyIsComplete(ingredients))
    }
  })
})

describe('buyCost', () => {
  it('multiplie le prix par la quantité de l\'objet', () => {
    expect(buyCost(workshopItem(AMULETTE, [], 3), prices(PRICES))).toBe(13_500_000)
  })
})

describe('mixedCost — les cas discutés', () => {
  const p = prices(PRICES)

  it('rien d\'obtenu, rien de crafté : achat partout', () => {
    expect(mixedCost(amulette(), p)).toEqual({
      total: { strategy: 'buy', cost: 4_500_000 },
      adjusted: { strategy: 'buy', cost: 4_500_000 },
    })
  })

  it('Hamatum récolté : le total reste sur l\'achat, l\'ajusté passe en craft', () => {
    expect(mixedCost(amulette({ hamatum: 1 }), p)).toEqual({
      total: { strategy: 'buy', cost: 4_500_000 },
      adjusted: { strategy: 'craft', cost: 4_310_000 },
    })
  })

  it('Ardonite craftée moins chère : craft partout', () => {
    expect(mixedCost(amulette({}, true), p)).toEqual({
      total: { strategy: 'craft', cost: 4_310_000 },
      adjusted: { strategy: 'craft', cost: 4_310_000 },
    })
  })

  it('Ardonite craftée plus chère : le meilleur prix l\'achète au lieu de la crafter', () => {
    //* Craft prévu : 4 610 000. En achetant l'Ardonite : 4 510 000, moins cher que l'amulette (4 600 000).
    const cher = prices({ ...PRICES, [SOUS_A]: 250_000, [AMULETTE]: 4_600_000 })
    expect(craftCostTotal(amulette({}, true).ingredients, cher)).toBe(4_610_000)
    expect(mixedCost(amulette({}, true), cher)).toEqual({
      total: { strategy: 'craft', cost: 4_510_000 },
      adjusted: { strategy: 'craft', cost: 4_510_000 },
    })
  })

  it('Ardonite craftée (1/1), minerais manquants : ses minerais restent à payer', () => {
    //* 200 000 + min(500 000, 300 000) + 3 810 000. Le 1/1 veut dire « je la crafte », pas « je l'ai ».
    const wi = amulette({}, true)
    expect(bestCraftCost(wi.ingredients, p)).toEqual({ total: 4_310_000, remaining: 4_310_000 })
    expect(mixedCost(wi, p).adjusted).toEqual({ strategy: 'craft', cost: 4_310_000 })
  })

  it('Ardonite craftée (1/1), minerais en partie obtenus : seuls les minerais restants sont payés', () => {
    //* SOUS_A obtenu : il reste 2 × 50 000 = 100 000 pour l'Ardonite.
    expect(bestCraftCost(amulette({ sousA: 2 }, true).ingredients, p).remaining).toBe(200_000 + 100_000 + 3_810_000)
  })

  it('Ardonite craftée (1/1), minerais tous obtenus : plus rien à payer pour elle', () => {
    expect(bestCraftCost(amulette({ sousA: 2, sousB: 2 }, true).ingredients, p).remaining).toBe(200_000 + 3_810_000)
  })

  it('Ardonite craftée plus chère, rien d\'obtenu : le meilleur prix ajusté l\'achète au prix plein', () => {
    const cher = prices({ ...PRICES, [SOUS_A]: 250_000 })
    //* min(500 000, 600 000) = 500 000, même si elle est à 1/1.
    expect(bestCraftCost(amulette({}, true).ingredients, cher).remaining).toBe(200_000 + 500_000 + 3_810_000)
  })

  it('Ardonite craftée plus chère, mais minerais en partie obtenus : finir son craft redevient moins cher', () => {
    const cher = prices({ ...PRICES, [SOUS_A]: 250_000 })
    //* SOUS_A obtenu : il reste 100 000 de minerais, contre 500 000 pour l'acheter.
    expect(bestCraftCost(amulette({ sousA: 2 }, true).ingredients, cher)).toEqual({
      total: 200_000 + 500_000 + 3_810_000,
      remaining: 200_000 + 100_000 + 3_810_000,
    })
  })

  it('la quantité obtenue d\'un ingrédient crafté ne change jamais le résultat (1/1 ou décochée)', () => {
    const coche = amulette({ sousA: 1 }, true)
    const decoche = { ...coche, ingredients: coche.ingredients.map(i => i.id === 2 ? { ...i, quantityObtained: 0 } : i) }
    expect(bestCraftCost(decoche.ingredients, p)).toEqual(bestCraftCost(coche.ingredients, p))
    expect(mixedCost(decoche, p)).toEqual(mixedCost(coche, p))
  })

  it('Ardonite craftée en quantité 3 : achat et minerais comptés × 3, sans re-multiplier', () => {
    const ingredients = [ing(1, HAMATUM, 3), ing(2, ARDONITE, 3, 3), ing(3, AUTRE, 3), ing(4, SOUS_A, 6, 0, 2), ing(5, SOUS_B, 6, 0, 2)]
    const best = bestCraftCost(ingredients, p)
    expect(best.total).toBe(3 * 200_000 + 3 * 300_000 + 3 * 3_810_000)
    expect(best.remaining).toBe(best.total)
  })

  it('plusieurs niveaux craftés (1/1) : les ressources du dernier niveau restent à payer', () => {
    //* 2 crafté (1/1) ; 3 sous 2, crafté (2/2) ; 4 sous 3 manquant ; 5 sous 2 manquant.
    const ingredients = [ing(1, 10, 1), ing(2, 11, 1, 1), ing(3, 20, 2, 2, 2), ing(4, 21, 4, 0, 3), ing(5, 22, 1, 0, 2)]
    const p2 = prices({ 10: 5, 11: 1_000, 20: 500, 21: 7, 22: 3 })
    expect(bestCraftCost(ingredients, p2)).toEqual({ total: 36, remaining: 36 })
  })

  it('ingrédient crafté sans prix d\'achat : son craft est pris', () => {
    const sansPrix = prices({ ...PRICES, [ARDONITE]: 0 })
    expect(bestCraftCost(amulette({}, true).ingredients, sansPrix).total).toBe(200_000 + 300_000 + 3_810_000)
  })

  it('ingrédient crafté dont le craft est inconnu : son achat est pris', () => {
    const sansMinerais = prices({ ...PRICES, [SOUS_A]: 0, [SOUS_B]: 0 })
    expect(bestCraftCost(amulette({}, true).ingredients, sansMinerais).total).toBe(4_510_000)
  })

  it('choisit à chaque niveau sur plusieurs niveaux de craft', () => {
    //* 2 → crafté (achat 1 000) ; 3 sous 2 → crafté (achat 500, craft 4 × 7 = 28) ; 4 sous 3.
    const ingredients = [ing(1, 10, 1), ing(2, 11, 1), ing(3, 20, 2, 0, 2), ing(4, 21, 4, 0, 3), ing(5, 22, 1, 0, 2)]
    const p2 = prices({ 10: 5, 11: 1_000, 20: 500, 21: 7, 22: 3 })
    //* Niveau 3 : min(500, 28) = 28 ; niveau 2 : min(1 000, 28 + 3) = 31 ; total 5 + 31.
    expect(bestCraftCost(ingredients, p2).total).toBe(36)
  })

  it('Ardonite craftée plus chère : achat partout', () => {
    const cher = prices({ ...PRICES, [SOUS_A]: 250_000 })
    expect(mixedCost(amulette({}, true), cher)).toEqual({
      total: { strategy: 'buy', cost: 4_500_000 },
      adjusted: { strategy: 'buy', cost: 4_500_000 },
    })
  })

  it('tout obtenu : l\'ajusté passe en craft à 0', () => {
    const complet = workshopItem(AMULETTE, [ing(1, HAMATUM, 1, 1), ing(2, ARDONITE, 1, 1), ing(3, AUTRE, 1, 1)])
    expect(mixedCost(complet, p).adjusted).toEqual({ strategy: 'craft', cost: 0 })
  })

  it('égalité : achat', () => {
    const egal = prices({ ...PRICES, [AUTRE]: 3_800_000 })
    expect(mixedCost(amulette(), egal).total).toEqual({ strategy: 'buy', cost: 4_500_000 })
  })

  it('objet sans recette : achat partout', () => {
    const sansRecette = workshopItem(AMULETTE, [], 2, false)
    expect(mixedCost(sansRecette, p)).toEqual({
      total: { strategy: 'buy', cost: 9_000_000 },
      adjusted: { strategy: 'buy', cost: 9_000_000 },
    })
  })

  it('prix d\'achat inconnu : le craft est pris', () => {
    const sansPrix = prices({ ...PRICES, [AMULETTE]: 0 })
    expect(mixedCost(amulette(), sansPrix)).toEqual({
      total: { strategy: 'craft', cost: 4_510_000 },
      adjusted: { strategy: 'craft', cost: 4_510_000 },
    })
  })

  it('prix de craft inconnu : l\'achat est pris, même si le reste à payer vaut 0', () => {
    const complet = workshopItem(AMULETTE, [ing(1, 99, 1, 1)])
    expect(mixedCost(complet, p)).toEqual({
      total: { strategy: 'buy', cost: 4_500_000 },
      adjusted: { strategy: 'buy', cost: 4_500_000 },
    })
  })

  it('les quantités déjà multipliées ne sont pas re-multipliées', () => {
    //* 3 amulettes : achat 13 500 000, ressources fournies déjà × 3.
    const trois = workshopItem(AMULETTE, [ing(1, HAMATUM, 3), ing(2, ARDONITE, 3), ing(3, AUTRE, 3)], 3)
    expect(mixedCost(trois, p).total).toEqual({ strategy: 'buy', cost: 13_500_000 })
    expect(craftCostTotal(trois.ingredients, p)).toBe(13_530_000)
  })
})

describe('mixedCost — invariants sur des ateliers aléatoires', () => {
  //* Générateur déterministe : un échec se rejoue à l'identique.
  const rng = (seed: number) => () => {
    seed = (seed * 1_103_515_245 + 12_345) % 2_147_483_648
    return seed / 2_147_483_648
  }

  const randomWorkshopItem = (rand: () => number) => {
    const ingredients: WorkshopItemIngredient[] = []
    const priceMap: Record<number, number> = { 1: Math.floor(rand() * 1_000_000) }
    let nextId = 1
    const addLevel = (parentId: number | undefined, depth: number) => {
      const count = 1 + Math.floor(rand() * 4)
      for (let i = 0; i < count; i++) {
        const id = nextId++
        const itemId = 100 + id
        const required = 1 + Math.floor(rand() * 20)
        const obtained = Math.floor(rand() * (required + 3))
        priceMap[itemId] = rand() < 0.1 ? 0 : Math.floor(rand() * 50_000)
        ingredients.push(ing(id, itemId, required, obtained, parentId))
        if (depth < 3 && rand() < 0.3) {
          //* Comme l'API : un ingrédient crafté passe à quantité requise = obtenue.
          ingredients[ingredients.length - 1].quantityObtained = required
          addLevel(id, depth + 1)
        }
      }
    }
    addLevel(undefined, 0)
    const quantity = 1 + Math.floor(rand() * 3)
    return { wi: workshopItem(1, ingredients, quantity, rand() > 0.05), priceOf: prices(priceMap) }
  }

  it('respecte les invariants sur 5 000 objets', () => {
    const rand = rng(42)
    for (let n = 0; n < 5_000; n++) {
      const { wi, priceOf } = randomWorkshopItem(rand)
      const { total, adjusted } = mixedCost(wi, priceOf)
      const buy = buyCost(wi, priceOf)
      const craftTotal = craftCostTotal(wi.ingredients, priceOf)
      const craftRemaining = craftCostRemaining(wi.ingredients, priceOf)
      const best = bestCraftCost(wi.ingredients, priceOf)
      const allPricesKnown = wi.ingredients.every(i => priceOf(i.item.id) > 0)

      //* Le reste à payer ne dépasse jamais le total.
      expect(craftRemaining).toBeLessThanOrEqual(craftTotal)
      //* Ce qui reste à payer ne dépasse jamais le coût en partant de zéro.
      expect(adjusted.cost).toBeLessThanOrEqual(total.cost)
      //* Un objet ne passe jamais du craft (total) à l'achat (ajusté).
      expect(total.strategy === 'craft' && adjusted.strategy === 'buy').toBe(false)
      //* Le coût annoncé est bien celui de la stratégie annoncée.
      expect(total.cost).toBe(total.strategy === 'buy' ? buy : best.total)
      expect(adjusted.cost).toBe(adjusted.strategy === 'buy' ? buy : best.remaining)
      //* Le meilleur reste à payer ne dépasse jamais le meilleur total.
      expect(best.remaining).toBeLessThanOrEqual(best.total)
      //* La quantité obtenue d'un ingrédient crafté n'a aucun effet : on la remet à 0 partout.
      const parents = new Set(wi.ingredients.map(i => i.parentIngredientId))
      const decoches = wi.ingredients.map(i => parents.has(i.id) ? { ...i, quantityObtained: 0 } : i)
      expect(bestCraftCost(decoches, priceOf)).toEqual(best)
      //* Sans aucune ressource de base obtenue, le reste à payer égale le total.
      const rienObtenu = wi.ingredients.map(i => parents.has(i.id) ? i : { ...i, quantityObtained: 0 })
      const bestRien = bestCraftCost(rienObtenu, priceOf)
      expect(bestRien.remaining).toBe(bestRien.total)
      //* Choisir à chaque niveau ne coûte jamais plus que le plan de craft prévu.
      if (allPricesKnown) {
        expect(best.total).toBeLessThanOrEqual(craftTotal)
        expect(best.remaining).toBeLessThanOrEqual(craftRemaining)
      }
      //* Quand les deux prix sont connus, c'est bien le moins cher (égalité → achat).
      if (wi.item.hasRecipe && buy > 0 && best.total > 0) {
        expect(total.cost).toBe(Math.min(buy, best.total))
        expect(adjusted.cost).toBe(Math.min(buy, best.remaining))
      }
    }
  })
})
