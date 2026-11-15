import { computed } from 'vue'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { useWorkshopPriceCalculator } from '@/modules/Dofus/workshop/composables/useWorkshopPriceCalculator'
import type { WorkshopItem, WorkshopItemIngredient, AggregatedResource, WorkshopSummary } from '@/modules/Dofus/workshop/types/workshop.types'
import { craftCostTotal, mixedCost } from '@/modules/Dofus/workshop/utils/workshopCost'
import { adjustedPrices, applyPreview } from '@/modules/Dofus/workshop/utils/workshopPreview'

export function useWorkshopSummary() {
  const workshopDetailStore = useWorkshopDetailStore()
  const { getUnitPrice } = useWorkshopPriceCalculator()

  const summary = computed<WorkshopSummary>(() => {
    const items = workshopDetailStore.items

    if (items.length === 0) {
      return createEmptySummary()
    }

    const resourceMap = new Map<number, AggregatedResource>()

    for (const workshopItem of items) {
      processIngredients(
        workshopItem.ingredients.filter(ing => !ing.parentIngredientId),
        1,
        workshopItem.ingredients,
        resourceMap,
        workshopItem
      )
    }

    const allResources = Array.from(resourceMap.values())

    const zoneMap = new Map<string, AggregatedResource[]>()

    for (const resource of allResources) {
      const zones = resource.item.farmZones || []
      
      if (zones.length === 0) {
        const existing = zoneMap.get('Zone inconnue') || []
        existing.push(resource)
        zoneMap.set('Zone inconnue', existing)
      } else {
        const primaryZone = zones.find(z => z.isPrimary) || zones[0]
        const zoneName = primaryZone.area.name

        const existing = zoneMap.get(zoneName) || []
        existing.push(resource)
        zoneMap.set(zoneName, existing)
      }
    }

    const sortedZones = Array.from(zoneMap.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([zoneName, resources]) => {
        const totalRequired = resources.reduce((sum, r) => sum + r.quantityRequired, 0)
        const totalObtained = resources.reduce((sum, r) => sum + r.quantityObtained, 0)
        const percent = totalRequired > 0 ? Math.round((totalObtained / totalRequired) * 100) : 0

        return {
          name: zoneName,
          percent,
          resources: resources.sort((a, b) => a.item.name.localeCompare(b.item.name))
        }
      })

    const totalResources = allResources.length
    const totalComplete = allResources.filter(r => r.quantityObtained >= r.quantityRequired).length
    const totalMissing = totalResources - totalComplete

    // Prix d'achat des items principaux
    const buyItemsCost = items.reduce((sum, item) => {
      const unitPrice = getUnitPrice(item.item.id)
      return sum + (unitPrice * item.quantity)
    }, 0)

    // Prix craft : ressources de base uniquement, un ingrédient crafté compte par ses sous-ingrédients
    const craftItems = items.filter(item => item.item.hasRecipe)
    const totalCraftCostTotal = craftItems.reduce((sum, item) => sum + craftCostTotal(item.ingredients, getUnitPrice), 0)

    // Prix mixte : le moins cher entre achat et craft, objet par objet
    const mixedTotal = items.reduce((sum, item) => sum + mixedCost(item, getUnitPrice).total.cost, 0)

    // Prix ajustés : achat (objets non terminés), craft restant, meilleur prix restant
    const adjusted = adjustedPrices(items, getUnitPrice)

    // Prévisions : les mêmes prix ajustés, si les ressources prévues étaient complètes
    const previewed = workshopDetailStore.previewedItemIds
    const preview = previewed.length > 0
      ? adjustedPrices(applyPreview(items, new Set(previewed)), getUnitPrice)
      : null

    return {
      totalItems: items.length,
      totalResources,
      totalMissing,
      totalComplete,
      buyItemsCost,
      buyItemsRemaining: adjusted.buyItemsRemaining,
      totalCost: 0, // DEPRECATED - sera supprimé du type
      craftCost: totalCraftCostTotal, // MODIFIÉ - maintenant craft total
      craftAdjusted: adjusted.craftAdjusted,
      mixedCost: mixedTotal,
      mixedAdjusted: adjusted.mixedAdjusted,
      preview,
      resources: allResources,
      zones: sortedZones
    }
  })

  function processIngredients(
    ingredients: WorkshopItemIngredient[],
    multiplier: number,
    allIngredients: WorkshopItemIngredient[],
    resourceMap: Map<number, AggregatedResource>,
    workshopItem: WorkshopItem
  ) {
    for (const ingredient of ingredients) {
      const totalRequired = ingredient.quantityRequired * multiplier
      const totalObtained = ingredient.quantityObtained * multiplier

      const unitPrice = getUnitPrice(ingredient.item.id)

      const existing = resourceMap.get(ingredient.item.id)

      if (existing) {
        existing.quantityRequired += totalRequired
        existing.quantityObtained += totalObtained
        existing.remainingToBuy = Math.max(
          0,
          existing.quantityRequired - existing.quantityObtained
        )
        existing.totalCost = existing.remainingToBuy * unitPrice

        if (!existing.usedIn.find(wi => wi.id === workshopItem.id)) {
          existing.usedIn.push(workshopItem)
        }
      } else {
        resourceMap.set(ingredient.item.id, {
          item: ingredient.item,
          quantityRequired: totalRequired,
          quantityObtained: totalObtained,
          remainingToBuy: Math.max(0, totalRequired - totalObtained),
          totalCost: Math.max(0, totalRequired - totalObtained) * unitPrice,
          usedIn: [workshopItem]
        })
      }

      if (ingredient.item.hasRecipe) {
        const subIngredients = allIngredients.filter(
          ing => ing.parentIngredientId === ingredient.id
        )

        if (subIngredients.length > 0) {
          processIngredients(
            subIngredients,
            multiplier,
            allIngredients,
            resourceMap,
            workshopItem
          )
        }
      }
    }
  }

  function createEmptySummary(): WorkshopSummary {
    return {
      totalItems: 0,
      totalResources: 0,
      totalMissing: 0,
      totalComplete: 0,
      buyItemsCost: 0,
      buyItemsRemaining: 0,
      totalCost: 0,
      craftCost: 0,
      craftAdjusted: 0,
      mixedCost: 0,
      mixedAdjusted: 0,
      preview: null,
      resources: [],
      zones: []
    }
  }

  return {
    summary
  }
}