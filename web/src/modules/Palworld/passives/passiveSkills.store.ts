import { defineStore } from 'pinia'
import { fetchPassiveSkills } from './fetch/passiveSkills.fetch'
import type { PalworldPassiveSkill } from './types/passiveSkills.types'

export const usePassiveSkillsStore = defineStore('palworld-passive-skills', {
  state: () => ({
    passiveSkills: [] as PalworldPassiveSkill[],
    loading: false,
    error: null as string | null,
  }),

  actions: {
    async ensureLoaded() {
      if (this.passiveSkills.length || this.loading) return

      this.loading = true
      this.error = null
      try {
        this.passiveSkills = await fetchPassiveSkills()
      } catch {
        this.error = 'Impossible de charger les passifs Palworld.'
      } finally {
        this.loading = false
      }
    },
  },
})
