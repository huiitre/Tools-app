import { describe, expect, it } from 'vitest'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import { adjustedPrices, applyPreview } from './workshopPreview'
import { mixedCost, type PriceOf } from './workshopCost'

const item = (id: number, hasRecipe = false) => ({ id, name: `#${id}`, hasRecipe }) as unknown as Item

const ing = (id: number, itemId: number, required: number, obtained = 0, parent?: number): WorkshopItemIngredient => ({
  id, workshopItemId: 1, item: item(itemId), parentIngredientId: parent, quantityRequired: required, quantityObtained: obtained,
})

const prices = (map: Record<number, number>): PriceOf => (id) => map[id] ?? 0

//* Atelier 10 local (« Trophées »), relevé le 15/11/2026 : Saccageur Air majeur, prix communautaires.
//* Les deux Saccageurs craftés sont à 1/1, comme l'API les met quand on choisit de les crafter.
const MAJEUR = 1, AIR = 2, MINEUR = 3
const RAYONNANT = 10, SOLAIRE = 11, KOBALITE = 12, FORET = 13, FEUILLE = 14, KATANA = 15, LAME = 16
const LUNAIRE = 20, RUTILANT = 21, KOUARTZ = 22, BOSQUET = 23, BEC = 24, BOULE = 25
const CRAMOISI = 30, SAISONNIER = 31, MAGNESITE = 32, FUTAIE = 33, MUQUEUSE = 34, DOPEUL = 35

const PRICES = prices({
  [MAJEUR]: 476_895, [AIR]: 226_552, [MINEUR]: 87_620,
  [RAYONNANT]: 204_587, [SOLAIRE]: 132_025, [KOBALITE]: 16_121, [FORET]: 14_217, [FEUILLE]: 193, [KATANA]: 281, [LAME]: 571,
  [LUNAIRE]: 30_267, [RUTILANT]: 41_807, [KOUARTZ]: 7_484, [BOSQUET]: 9_234, [BEC]: 868, [BOULE]: 1_444,
  [CRAMOISI]: 5_128, [SAISONNIER]: 4_606, [MAGNESITE]: 4_039, [FUTAIE]: 6_197, [MUQUEUSE]: 2_849, [DOPEUL]: 6_846,
})

const saccageur = (): WorkshopItem => ({
  id: 1, workshopId: 10, quantity: 1, item: item(MAJEUR, true),
  ingredients: [
    ing(455, RAYONNANT, 1), ing(452, SOLAIRE, 1), ing(448, KOBALITE, 1), ing(451, FORET, 1),
    ing(453, FEUILLE, 5), ing(449, KATANA, 5), ing(450, LAME, 10),
    ing(454, AIR, 1, 1),
    ing(461, LUNAIRE, 1, 0, 454), ing(459, RUTILANT, 1, 0, 454), ing(456, KOUARTZ, 1, 0, 454),
    ing(460, BOSQUET, 1, 0, 454), ing(458, BEC, 5, 0, 454), ing(457, BOULE, 5, 0, 454),
    ing(462, MINEUR, 1, 1, 454),
    ing(467, CRAMOISI, 1, 0, 462), ing(468, SAISONNIER, 1, 0, 462), ing(463, MAGNESITE, 1, 0, 462),
    ing(466, FUTAIE, 1, 0, 462), ing(464, MUQUEUSE, 5, 0, 462), ing(465, DOPEUL, 5, 0, 462),
  ],
})

describe('adjustedPrices', () => {
  it('redonne les valeurs affichées par l\'atelier 10 (vérifiées à la main)', () => {
    expect(adjustedPrices([saccageur()], PRICES)).toEqual({
      buyItemsRemaining: 476_895,
      craftAdjusted: 543_827,
      mixedAdjusted: 476_895,
    })
  })

  it('n\'ajoute au craft ajusté que les objets qui ont une recette, et au meilleur prix tous les objets', () => {
    const sansRecette: WorkshopItem = { id: 2, workshopId: 10, quantity: 2, item: item(99), ingredients: [] }
    const p = prices({ 99: 1_000 })
    //* Sans ingrédient, l'objet est « terminé » : il sort de l'achat ajusté, comme avant.
    expect(adjustedPrices([sansRecette], p)).toEqual({ buyItemsRemaining: 0, craftAdjusted: 0, mixedAdjusted: 2_000 })
  })
})

describe('applyPreview', () => {
  it('sans prévision, les prix prévus sont identiques aux vrais', () => {
    const items = [saccageur()]
    expect(adjustedPrices(applyPreview(items, new Set()), PRICES)).toEqual(adjustedPrices(items, PRICES))
  })

  it('ne modifie jamais les objets d\'origine', () => {
    const items = [saccageur()]
    const before = structuredClone(items)
    applyPreview(items, new Set([RAYONNANT, CRAMOISI, AIR]))
    expect(items).toEqual(before)
  })

  it('prévoir le Galet rayonnant : le craft passe sous l\'achat et le meilleur prix bascule en craft', () => {
    const preview = applyPreview([saccageur()], new Set([RAYONNANT]))
    //* 543 827 − 204 587 = 339 240, moins cher que l'achat (476 895).
    expect(adjustedPrices(preview, PRICES)).toEqual({
      buyItemsRemaining: 476_895,
      craftAdjusted: 339_240,
      mixedAdjusted: 339_240,
    })
    expect(mixedCost(preview[0], PRICES).adjusted.strategy).toBe('craft')
  })

  it('prévoir une petite ressource : le meilleur prix reste sur l\'achat, sans baisser', () => {
    const preview = applyPreview([saccageur()], new Set([LAME]))
    expect(adjustedPrices(preview, PRICES)).toEqual({
      buyItemsRemaining: 476_895,
      craftAdjusted: 543_827 - 10 * 571,
      mixedAdjusted: 476_895,
    })
  })

  it('prévoir une ressource d\'un ingrédient crafté (dernier niveau) la retire bien du craft ajusté', () => {
    const preview = applyPreview([saccageur()], new Set([DOPEUL]))
    expect(adjustedPrices(preview, PRICES).craftAdjusted).toBe(543_827 - 5 * 6_846)
  })

  it('prévoir un ingrédient crafté n\'a aucun effet : seules ses ressources comptent', () => {
    const items = [saccageur()]
    expect(adjustedPrices(applyPreview(items, new Set([AIR, MINEUR])), PRICES)).toEqual(adjustedPrices(items, PRICES))
  })

  it('une ressource en partie obtenue est complétée (18 / 59 → 59 / 59), jamais réduite', () => {
    const items: WorkshopItem[] = [{
      id: 1, workshopId: 1, quantity: 1, item: item(1, true),
      ingredients: [ing(1, 50, 59, 18), ing(2, 51, 3, 5)],
    }]
    const [preview] = applyPreview(items, new Set([50, 51]))
    expect(preview.ingredients.map(i => i.quantityObtained)).toEqual([59, 5])
    //* Restant de la Malléole : 41 × 322 = 13 202, retiré du craft ajusté.
    const p = prices({ 1: 1_000_000, 50: 322, 51: 10 })
    expect(adjustedPrices(items, p).craftAdjusted - adjustedPrices([preview], p).craftAdjusted).toBe(13_202)
  })

  it('une ressource utilisée par plusieurs objets est prévue dans chacun', () => {
    const items: WorkshopItem[] = [
      { id: 1, workshopId: 1, quantity: 1, item: item(1, true), ingredients: [ing(1, 50, 4), ing(2, 51, 1)] },
      { id: 2, workshopId: 1, quantity: 1, item: item(2, true), ingredients: [ing(3, 50, 6, 2), ing(4, 52, 1)] },
    ]
    const preview = applyPreview(items, new Set([50]))
    expect(preview.flatMap(i => i.ingredients).filter(i => i.item.id === 50).map(i => i.quantityObtained)).toEqual([4, 6])
  })

  it('tout prévoir : l\'objet est terminé, plus rien à payer', () => {
    const items = [saccageur()]
    const all = new Set(items[0].ingredients.map(i => i.item.id))
    expect(adjustedPrices(applyPreview(items, all), PRICES)).toEqual({
      buyItemsRemaining: 0,
      craftAdjusted: 0,
      mixedAdjusted: 0,
    })
  })
})
