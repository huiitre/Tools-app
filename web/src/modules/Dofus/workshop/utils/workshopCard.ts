import type { WorkshopItem } from '@/modules/Dofus/workshop/types/workshop.types'
import { mixedCost, type PriceOf } from '@/modules/Dofus/workshop/utils/workshopCost'

export type WorkshopCardStats = {
  itemCount: number
  minLevel: number | null
  maxLevel: number | null
  //* Mêmes chiffres que « Meilleur prix » et « Meilleur prix ajusté » du résumé de l'atelier.
  total: number
  adjusted: number
}

export function workshopCardStats(items: WorkshopItem[], priceOf: PriceOf): WorkshopCardStats {
  const levels = items.map(item => item.item.level)

  return {
    itemCount: items.length,
    minLevel: levels.length ? Math.min(...levels) : null,
    maxLevel: levels.length ? Math.max(...levels) : null,
    total: items.reduce((sum, item) => sum + mixedCost(item, priceOf).total.cost, 0),
    adjusted: items.reduce((sum, item) => sum + mixedCost(item, priceOf).adjusted.cost, 0),
  }
}
