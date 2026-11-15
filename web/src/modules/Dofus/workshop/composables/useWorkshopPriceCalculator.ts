// composables/useWorkshopPriceCalculator.ts
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'
import { useDofusConfigStore } from '@/modules/Dofus/preferences/preferences.store'
import { getItemPriceByMode } from '@/modules/Dofus/item/utils/itemPriceSelector'
import type { WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'

export function useWorkshopPriceCalculator() {
  const { get: getPrice } = useItemPrices()
  const dofusConfigStore = useDofusConfigStore()

  /**
   * Récupère le prix unitaire d'un item selon le mode de prix configuré
   */
  function getUnitPrice(itemId: number): number {
    const p = getPrice(itemId)
    return p ? getItemPriceByMode(p, dofusConfigStore.priceDisplayMode) : 0
  }

  /**
   * Calcule le prix craft d'un item = somme PU des ingrédients directs NON craftés
   * quantityRequired est DÉJÀ multiplié par le backend, pas de re-multiplication
   */
  function calculateItemCraftCost(ingredients: WorkshopItemIngredient[]): number {
    const directIngredients = ingredients.filter(ing => !ing.parentIngredientId)
    
    return directIngredients.reduce((sum, ing) => {
      const isCrafted = ingredients.some(sub => sub.parentIngredientId === ing.id)
      if (isCrafted) return sum
      
      return sum + (getUnitPrice(ing.item.id) * ing.quantityRequired)
    }, 0)
  }

  /**
   * Calcule le prix craft d'un ingrédient crafté spécifique (pour les cards)
   */
  function calculateCraftedIngredientCraftCost(
    allIngredients: WorkshopItemIngredient[],
    craftedIngredientId: number
  ): number {
    const subIngredients = allIngredients.filter(
      sub => sub.parentIngredientId === craftedIngredientId
    )
    
    return subIngredients.reduce((sum, sub) => {
      return sum + (getUnitPrice(sub.item.id) * sub.quantityRequired)
    }, 0)
  }

  /**
   * Calcule le prix craft ajusté d'un ingrédient crafté (pour les cards)
   */
  function calculateCraftedIngredientCraftAdjusted(
    allIngredients: WorkshopItemIngredient[],
    craftedIngredientId: number
  ): number {
    const subIngredients = allIngredients.filter(
      sub => sub.parentIngredientId === craftedIngredientId
    )
    
    return subIngredients.reduce((sum, sub) => {
      const remaining = Math.max(0, sub.quantityRequired - sub.quantityObtained)
      return sum + (getUnitPrice(sub.item.id) * remaining)
    }, 0)
  }

  return {
    getUnitPrice,
    calculateItemCraftCost,
    calculateCraftedIngredientCraftCost,
    calculateCraftedIngredientCraftAdjusted
  }
}