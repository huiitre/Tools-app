import { describe, expect, it } from 'vitest'
import type { Item } from '@/modules/Dofus/item/types/item.types'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import { workshopCardStats } from './workshopCard'

const item = (id: number, level: number) => ({ id, name: `#${id}`, hasRecipe: true, level }) as unknown as Item

const ing = (id: number, itemId: number, quantityRequired: number, quantityObtained = 0): WorkshopItemIngredient => ({
  id,
  workshopItemId: 1,
  item: { id: itemId, name: `#${itemId}`, hasRecipe: false } as unknown as Item,
  quantityRequired,
  quantityObtained,
})

const workshopItem = (itemId: number, level: number, ingredients: WorkshopItemIngredient[]): WorkshopItem =>
  ({ id: itemId, workshopId: 1, item: item(itemId, level), quantity: 1, ingredients })

describe('workshopCardStats', () => {
  it('atelier vide : aucun niveau, prix nuls', () => {
    expect(workshopCardStats([], () => 0)).toEqual({ itemCount: 0, minLevel: null, maxLevel: null, total: 0, adjusted: 0 })
  })

  it('niveaux min et max, meilleur prix total et ajusté', () => {
    // Objet 1 (niv. 50) : achat 1 000 ou craft 10 × 50 = 500 → 500 ; 4 ressources déjà là → ajusté 300.
    // Objet 2 (niv. 120) : achat 200, craft 5 × 100 = 500 → 200, rien d'obtenu → ajusté 200.
    const prices: Record<number, number> = { 1: 1_000, 2: 200, 10: 50, 20: 100 }
    const stats = workshopCardStats(
      [workshopItem(1, 50, [ing(1, 10, 10, 4)]), workshopItem(2, 120, [ing(2, 20, 5)])],
      id => prices[id] ?? 0,
    )

    expect(stats).toEqual({ itemCount: 2, minLevel: 50, maxLevel: 120, total: 700, adjusted: 500 })
  })
})
