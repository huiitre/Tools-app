import type { WorkshopItem } from '@/modules/Dofus/workshop/types/workshop.types'
import { baseIngredients, craftCostRemaining, isComplete, mixedCost, type PriceOf } from './workshopCost'

//* Prévisions : « si ces ressources étaient complètes ». Rien n'est enregistré ni envoyé : on
//* calcule les prix ajustés sur une copie de l'atelier où les ressources prévues sont complètes,
//* avec exactement les mêmes fonctions que pour les vrais prix.

export type AdjustedPrices = {
  buyItemsRemaining: number
  craftAdjusted: number
  mixedAdjusted: number
}

//* Les trois prix ajustés du résumé.
export function adjustedPrices(items: WorkshopItem[], priceOf: PriceOf): AdjustedPrices {
  return {
    buyItemsRemaining: items
      .filter(item => !isComplete(item.ingredients))
      .reduce((sum, item) => sum + priceOf(item.item.id) * item.quantity, 0),
    craftAdjusted: items
      .filter(item => item.item.hasRecipe)
      .reduce((sum, item) => sum + craftCostRemaining(item.ingredients, priceOf), 0),
    mixedAdjusted: items
      .reduce((sum, item) => sum + mixedCost(item, priceOf).adjusted.cost, 0),
  }
}

//* Copie de l'atelier où chaque ressource de base prévue est complète, dans tous les objets qui
//* l'utilisent. Les ingrédients craftés ne sont pas touchés (leur 1/1 n'a aucun effet sur les prix).
//* Les objets d'origine ne sont jamais modifiés.
export function applyPreview(items: WorkshopItem[], previewedItemIds: ReadonlySet<number>): WorkshopItem[] {
  return items.map(item => {
    const bases = new Set(baseIngredients(item.ingredients).map(ing => ing.id))
    return {
      ...item,
      ingredients: item.ingredients.map(ing =>
        bases.has(ing.id) && previewedItemIds.has(ing.item.id)
          ? { ...ing, quantityObtained: Math.max(ing.quantityObtained, ing.quantityRequired) }
          : ing,
      ),
    }
  })
}
