import { defineStore } from 'pinia'
import { fetchGameServers } from '../fetch/gameServers.fetch'
import type { GameServer } from '../types/gameServers.types'

const REFRESH_INTERVAL_MS = 60_000
// Au-delà, les plus anciennes lignes du journal tombent.
const MAX_LOG_LINES = 500

export const useGameServersStore = defineStore('gameServers', {
  state: () => ({
    servers: [] as GameServer[],
    loading: false,
    error: null as string | null,
    refreshTimer: null as ReturnType<typeof setInterval> | null,
    // Journal cumulé par slug : Ark vide le sien à la lecture, chaque appel n'en rend que la suite.
    logs: {} as Record<string, string[]>,
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

    appendLog(slug: string, lines: string[]) {
      if (!lines.length) return
      this.logs[slug] = [...(this.logs[slug] ?? []), ...lines].slice(-MAX_LOG_LINES)
    },

    clearLog(slug: string) {
      delete this.logs[slug]
    },

    stopAutoRefresh() {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
  },
})
