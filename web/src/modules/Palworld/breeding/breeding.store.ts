import { defineStore } from 'pinia'
import { usePaldexStore } from '../paldex/paldex.store'
import type { PalworldPalListItem } from '../paldex/types/paldex.types'

// Sélection partagée entre toutes les vues du module Breeding (Calculateur, Recherche, et plus tard
// Path finder / Arbre) : évite de dupliquer la résolution route.query.pal -> PalworldPalListItem dans
// chaque vue, et fait persister la sélection en changeant d'onglet même sans passer par l'URL.
// L'URL reste la source de vérité au chargement/partage de lien — chaque vue lit sa propre
// route.query.pal au montage et écrit dedans quand la sélection change, ce store n'est qu'un relais.
export const useBreedingStore = defineStore('breeding', {
  state: () => ({
    selectedPalId: null as number | null,
  }),

  getters: {
    selectedPal(state): PalworldPalListItem | null {
      if (state.selectedPalId === null) return null
      const paldexStore = usePaldexStore()
      return paldexStore.pals.find(p => p.id === state.selectedPalId) ?? null
    },
  },

  actions: {
    selectPal(pal: PalworldPalListItem | null) {
      this.selectedPalId = pal?.id ?? null
    },
  },
})
