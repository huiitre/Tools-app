import { defineStore } from 'pinia'
import type { Almanax } from '@/modules/Dofus/almanax/types/almanax.types'
import { useFetchAlmanax, fetchAlmanaxSubscriptions, addAlmanaxSubscription, removeAlmanaxSubscription } from '@/modules/Dofus/almanax/fetch/almanax.fetch'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'

type AlmanaxState = {
  almanaxList: Almanax[]
  subscribedAlmanaxIds: Set<number>
  loading: boolean
  error: string | null
}

export const useAlmanaxStore = defineStore('dofus.almanax', {
  state: (): AlmanaxState => ({
    almanaxList: [],
    subscribedAlmanaxIds: new Set(),
    loading: false,
    error: null,
  }),

  getters: {
    almanaxByDate(state): Map<string, Almanax> {
      const map = new Map<string, Almanax>()
      for (const a of state.almanaxList) {
        map.set(a.date, a)
      }
      return map
    },

    minDate(state): Date | null {
      if (state.almanaxList.length === 0) return null
      const dates = state.almanaxList.map(a => a.date).sort()
      return new Date(dates[0])
    },

    maxDate(state): Date | null {
      if (state.almanaxList.length === 0) return null
      const dates = state.almanaxList.map(a => a.date).sort()
      return new Date(dates[dates.length - 1])
    },
  },

  actions: {
    async fetch() {
      if (this.loading) return

      this.loading = true
      this.error = null

      try {
        const { data } = await useFetchAlmanax()
        this.almanaxList = data

      } catch (e: any) {
        this.error = e?.message ?? 'Erreur lors du chargement des données Almanax'
        throw e
      } finally {
        this.loading = false
      }

      try {

        if (this.almanaxList.length === 0) return;

        const { load: loadItemPrices } = useItemPrices()
        const itemIds: number[] = Array.from(
          new Set(
            this.almanaxList
              .map((almanax: Almanax) => almanax.item?.id)
              .filter((id: number | undefined): id is number => typeof id === 'number')
          )
        )

        await loadItemPrices(itemIds)

      } catch (e: any) {

      }
    },

    async fetchSubscriptions() {
      try {
        const ids = await fetchAlmanaxSubscriptions()
        this.subscribedAlmanaxIds = new Set(ids)
      } catch {
        // silencieux — les abonnements sont non critiques
      }
    },

    async toggleSubscription(almanaxId: number, date: string) {
      if (this.subscribedAlmanaxIds.has(almanaxId)) {
        this.subscribedAlmanaxIds.delete(almanaxId)
        try {
          await removeAlmanaxSubscription(almanaxId)
        } catch {
          this.subscribedAlmanaxIds.add(almanaxId)
        }
      } else {
        this.subscribedAlmanaxIds.add(almanaxId)
        try {
          await addAlmanaxSubscription(almanaxId, date)
        } catch {
          this.subscribedAlmanaxIds.delete(almanaxId)
        }
      }
    },

    clear() {
      this.almanaxList = []
      this.subscribedAlmanaxIds = new Set()
      this.loading = false
      this.error = null
    },
  },
})