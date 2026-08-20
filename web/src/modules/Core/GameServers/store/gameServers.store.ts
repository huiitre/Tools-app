import { defineStore } from 'pinia'
import { fetchGameServers } from '../fetch/gameServers.fetch'
import type { GameServer } from '../types/gameServers.types'

const REFRESH_INTERVAL_MS = 60_000

export const useGameServersStore = defineStore('gameServers', {
  state: () => ({
    servers: [] as GameServer[],
    loading: false,
    error: null as string | null,
    refreshTimer: null as ReturnType<typeof setInterval> | null,
  }),

  getters: {
    hasOnlineServer: state => state.servers.some(server => server.online === true),
  },

  actions: {
    async load() {
      this.loading = true
      this.error = null
      try {
        this.servers = await fetchGameServers()
      } catch {
        this.error = 'Impossible de charger les serveurs de jeux.'
      } finally {
        this.loading = false
      }
    },

    async ensureLoaded() {
      if (this.servers.length || this.loading) return
      await this.load()
    },

    startAutoRefresh() {
      if (this.refreshTimer) return
      this.refreshTimer = setInterval(() => this.load(), REFRESH_INTERVAL_MS)
    },
  },
})
