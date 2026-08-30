import { defineStore } from 'pinia'
import { fetchTemtem, fetchTemtemDetail, fetchTemtemTypes } from './fetch/temtemdex.fetch'
import type { TemtemDetail, TemtemSummary, TemtemType } from '@/modules/Temtem/shared/types/temtem.types'

/**
 * Catalogue chargé une fois à l'entrée sur /temtem et gardé en mémoire, comme le Paldex :
 * 165 Temtem et 12 types tiennent en deux appels, et « Mes équipes » puis le simulateur les
 * reliront sans y retoucher.
 */
export const useTemtemdexStore = defineStore('temtemdex', {
  state: () => ({
    temtem: [] as TemtemSummary[],
    types: [] as TemtemType[],
    // Fiches déjà lues, par slug. Le choix des techniques d'une équipe en demande une par
    // membre : les garder évite de rappeler l'API à chaque ouverture du sélecteur.
    details: {} as Record<string, TemtemDetail>,
    loading: false,
    error: null as string | null,
  }),

  getters: {
    /**
     * Échelle commune des barres de statistiques : la plus haute valeur du catalogue, toutes
     * statistiques confondues (125 aujourd'hui). Mesurer chaque Temtem contre lui-même
     * remplirait la barre de sa meilleure statistique quelle qu'elle soit — une attaque à 55
     * paraîtrait maximale. Les sept amplitudes sont assez proches (max 92 à 125) pour qu'une
     * seule échelle les serve toutes.
     */
    maxStatValue(state): number {
      let max = 0
      for (const temtem of state.temtem) {
        for (const value of Object.values(temtem.stats)) {
          if (value > max) max = value
        }
      }
      // Jamais zéro : le catalogue peut n'être pas encore chargé quand la vue se monte.
      return max || 1
    },
  },

  actions: {
    async ensureLoaded() {
      if (this.temtem.length || this.loading) return
      this.loading = true
      this.error = null
      try {
        const [temtem, types] = await Promise.all([fetchTemtem(), fetchTemtemTypes()])
        this.temtem = temtem
        this.types = types
      } catch {
        this.error = 'Impossible de charger le Temtemdex.'
      } finally {
        this.loading = false
      }
    },

    async ensureDetail(slug: string): Promise<TemtemDetail> {
      const cached = this.details[slug]
      if (cached) return cached

      const detail = await fetchTemtemDetail(slug)
      this.details = { ...this.details, [slug]: detail }
      return detail
    },
  },
})
