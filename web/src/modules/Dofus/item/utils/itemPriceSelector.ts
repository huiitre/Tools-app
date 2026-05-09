import type { ItemPrice } from '@/modules/Dofus/item/types/item.types'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'

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

function formatRelativeTime(date: Date): string {
  const ms = Date.now() - new Date(date).getTime()
  const minutes = Math.floor(ms / (1000 * 60))
  const hours = Math.floor(ms / (1000 * 60 * 60))
  const days = Math.floor(ms / (1000 * 60 * 60 * 24))
  const weeks = Math.floor(days / 7)
  const months = Math.floor(days / 30)
  const years = Math.floor(days / 365)

  if (minutes < 1) return "à l'instant"
  if (minutes < 60) return `il y a ${minutes} minute${minutes > 1 ? 's' : ''}`
  if (hours < 24) return `il y a ${hours} heure${hours > 1 ? 's' : ''}`
  if (days < 7) return `il y a ${days} jour${days > 1 ? 's' : ''}`
  if (weeks < 4) return `il y a ${weeks} semaine${weeks > 1 ? 's' : ''}`
  if (months < 12) return `il y a ${months} mois`
  return `il y a ${years} an${years > 1 ? 's' : ''}`
}
