import type { ItemPrice } from '@/modules/Dofus/item/types/item.types'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'
import { formatRelativeTime } from '@/utils/formatRelativeTime'

export enum PriceAgeStatus {
  FRESH = 'fresh',
  WARNING = 'warning',
  DANGER = 'danger',
}

export function getItemPriceByMode(prices: ItemPrice, mode: PriceDisplayMode): number {
  if (!prices) {
    return 0
  }

  switch (mode) {
    case PriceDisplayMode.USER:
      return prices.userPrice ?? 0
    case PriceDisplayMode.COMMUNITY:
      return prices.communityAveragePrice ?? 0
    case PriceDisplayMode.LAST:
      return prices.lastUpdatedPrice ?? 0
    default:
      return 0
  }
}

export function getItemPriceDateByMode(prices: ItemPrice, mode: PriceDisplayMode): string | null {
  if (!prices) return null

  let date: Date | null = null

  switch (mode) {
    case PriceDisplayMode.USER:
      date = prices.userPriceCreatedAt ?? null
      break
    case PriceDisplayMode.COMMUNITY:
      date = prices.communityAveragePriceCreatedAt ?? null
      break
    case PriceDisplayMode.LAST:
      date = prices.lastUpdatedPriceCreatedAt ?? null
      break
  }

  return date ? formatRelativeTime(date) : null
}

export function getPriceAgeStatus(prices: ItemPrice, mode: PriceDisplayMode): PriceAgeStatus | null {
  if (!prices) return null

  let date: Date | null = null

  switch (mode) {
    case PriceDisplayMode.USER:
      date = prices.userPriceCreatedAt ?? null
      break
    case PriceDisplayMode.COMMUNITY:
      date = prices.communityAveragePriceCreatedAt ?? null
      break
    case PriceDisplayMode.LAST:
      date = prices.lastUpdatedPriceCreatedAt ?? null
      break
  }

  if (!date) return null

  const days = (Date.now() - new Date(date).getTime()) / (1000 * 60 * 60 * 24)

  if (days < 1) return PriceAgeStatus.FRESH
  if (days < 3) return PriceAgeStatus.WARNING
  return PriceAgeStatus.DANGER
}
