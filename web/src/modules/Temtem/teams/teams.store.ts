import { defineStore } from 'pinia'
import {
  addTeamMember,
  createTeam,
  deleteTeam,
  fetchTeams,
  removeTeamMember,
  reorderTeamMembers,
  renameTeam,
  setMemberTechniques,
} from './fetch/teams.fetch'
import type { TemtemTeam } from './types/teams.types'

/**
 * Équipes de l'utilisateur. Chaque écriture de l'API rend l'équipe entière : on remplace la
 * ligne concernée avec ce qu'elle renvoie, sans jamais recharger la liste ni reconstituer
 * l'état de son côté.
 */
export const useTemtemTeamsStore = defineStore('temtemTeams', {
  state: () => ({
    teams: [] as TemtemTeam[],
    loading: false,
    error: null as string | null,
  }),

  actions: {
    async ensureLoaded() {
      if (this.teams.length || this.loading) return
      await this.reload()
    },

    async reload() {
      this.loading = true
      this.error = null
      try {
        this.teams = await fetchTeams()
      } catch {
        this.error = 'Impossible de charger vos équipes.'
      } finally {
        this.loading = false
      }
    },

    async create(name: string, temtemId?: number) {
      const team = await createTeam(name, temtemId)
      this.teams = [...this.teams, team].sort((a, b) => a.name.localeCompare(b.name))
      return team
    },

    async addMember(teamId: number, temtemId: number, slot?: number) {
      return this.replace(await addTeamMember(teamId, temtemId, slot))
    },

    async reorderMembers(teamId: number, memberIds: number[]) {
      return this.replace(await reorderTeamMembers(teamId, memberIds))
    },

    async rename(teamId: number, name: string) {
      const team = await renameTeam(teamId, name)
      this.teams = this.teams
        .map(existing => (existing.id === team.id ? team : existing))
        .sort((a, b) => a.name.localeCompare(b.name))
      return team
    },

    async remove(teamId: number) {
      await deleteTeam(teamId)
      this.teams = this.teams.filter(existing => existing.id !== teamId)
    },

    async removeMember(teamId: number, memberId: number) {
      return this.replace(await removeTeamMember(teamId, memberId))
    },

    async setTechniques(teamId: number, memberId: number, techniqueIds: number[]) {
      return this.replace(await setMemberTechniques(teamId, memberId, techniqueIds))
    },

    // L'API rend l'équipe entière après chaque écriture : on la substitue telle quelle plutôt
    // que de rejouer la modification de notre côté.
    replace(team: TemtemTeam) {
      this.teams = this.teams.map(existing => (existing.id === team.id ? team : existing))
      return team
    },
  },
})
