import { ref, watch } from 'vue'
import type { ItemPrice } from '@/modules/Dofus/item/types/item.types'
import { useFetchItemPrices } from '@/modules/Dofus/item/fetch/item.fetch'

const prices = ref<Map<number, ItemPrice>>(new Map())

const trackedItemIds = ref<number[]>([])

let refreshTimer: number | null = null

export function useItemPrices() {

  const load = async (itemIds: number[]) => {
    if (itemIds.length === 0) return

    //? Filtre les ids déjà en cache
    const newIds = itemIds.filter(id => !prices.value.has(id))
    
    if (newIds.length === 0) return

    trackedItemIds.value = Array.from(
      new Set([...trackedItemIds.value, ...newIds])
    )

    const { data }: { data: ItemPrice[] } = await useFetchItemPrices(newIds)

    for (const entry of data) {
      prices.value.set(entry.itemId, entry)
    }

    prices.value = new Map(prices.value)
  }

  const refresh = async (itemIds?: number[]) => {
    // Fallback : refresh tout le cache
    if (!itemIds || itemIds.length === 0) {
      const allIds = Array.from(prices.value.keys())
      if (allIds.length === 0) return

      const { data }: { data: ItemPrice[] } = await useFetchItemPrices(allIds)
      for (const entry of data) {
        prices.value.set(entry.itemId, entry)
      }
      prices.value = new Map(prices.value)
      return
    }

    const idsToRefresh = new Set<number>()

    for (const id of itemIds) {
      idsToRefresh.add(id)

      const price = prices.value.get(id)
      if (price?.parentItemIds?.length) {
        for (const parentId of price.parentItemIds) {
          idsToRefresh.add(parentId)
        }
      }
    }

    const ids = Array.from(idsToRefresh)
    if (ids.length === 0) return

    const { data }: { data: ItemPrice[] } = await useFetchItemPrices(ids)

    for (const entry of data) {
      prices.value.set(entry.itemId, entry)
    }

    prices.value = new Map(prices.value)
  }

  const refreshRecursive = async (itemIds?: number[]) => {

    const startIds =
      itemIds && itemIds.length > 0
        ? itemIds
        : Array.from(prices.value.keys())

    if (startIds.length === 0) return

    const toVisit = new Set<number>()
    const visited = new Set<number>()

    for (const id of startIds) {
      toVisit.add(id)
    }

    while (toVisit.size > 0) {
      const iterator = toVisit.values().next()
      if (iterator.done) break

      const current: number = iterator.value
      toVisit.delete(current)

      if (visited.has(current)) continue
      visited.add(current)

      const price = prices.value.get(current)
      const parentIds = price?.parentItemIds ?? []
      for (const parentId of parentIds) {
        if (!visited.has(parentId)) {
          toVisit.add(parentId)
        }
      }
    }

    const ids = Array.from(visited)
    if (ids.length === 0) return

    const { data }: { data: ItemPrice[] } = await useFetchItemPrices(ids)

    for (const entry of data) {
      prices.value.set(entry.itemId, entry)
    }

    prices.value = new Map(prices.value)
  }

  const startAutoRefresh = (intervalMs = 5 * 60 * 1000) => {
    if (refreshTimer !== null) return

    refreshTimer = window.setInterval(async () => {
      if (trackedItemIds.value.length === 0) return
      await load(trackedItemIds.value)
    }, intervalMs)
  }

  const stopAutoRefresh = () => {
    if (refreshTimer !== null) {
      clearInterval(refreshTimer)
      refreshTimer = null
    }
  }

  const set = (itemId: number, price: number) => {
    const current = prices.value.get(itemId)
    if (!current) return

    prices.value.set(itemId, {
      ...current,
      userPrice: price,
    })

    prices.value = new Map(prices.value)
  }

  const get = (itemId: number): ItemPrice | undefined => {
    return prices.value.get(itemId)
  }

  const clear = () => {
    prices.value.clear()
    prices.value = new Map()
    trackedItemIds.value = []
    stopAutoRefresh()
  }

  watch(
    prices,
    (newPrices) => {
      /* console.log(
        'prices updated:',
        prices.value,
      ) */
    },
    { deep: false }
  )

  return {
    prices,
    load,
    get,
    startAutoRefresh,
    stopAutoRefresh,
    clear,
    refresh,
    refreshRecursive,
    set,
  }
}
