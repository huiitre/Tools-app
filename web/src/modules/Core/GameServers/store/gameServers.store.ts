import { defineStore } from 'pinia'
import { fetchGameServers, fetchGameServersLiveState } from '../fetch/gameServers.fetch'
import { coreHubConnection } from '@/modules/Core/Realtime/infrastructure/coreHubConnection'
import type { GameServer, GameServerLiveSnapshot } from '../types/gameServers.types'

const LIVE_UPDATED_EVENT = 'Core.GameServersLiveUpdated'
// Au-delà, les plus anciennes lignes du journal tombent.
const MAX_LOG_LINES = 500

export const useGameServersStore = defineStore('gameServers', {
  state: () => ({
    // Info statique (sync manifest) : nom, image, mods — ne change quasiment jamais.
    staticServers: [] as GameServer[],
    // État live (poll + push WebSocket), par slug — voir PollGameServersUseCase côté API.
    liveBySlug: {} as Record<string, GameServerLiveSnapshot>,
    loading: false,
    error: null as string | null,
    liveSubscribed: false,
    // Journal cumulé par slug, alimenté par chaque snapshot poussé (indépendant qu'un dashboard
    // soit ouvert ou non) : Ark vide le sien à la lecture, chaque snapshot n'en porte que la suite.
    logs: {} as Record<string, string[]>,
  }),

  getters: {
    // Fusionne l'info statique et l'état live : le reste du front continue de lire `servers`
    // sans savoir que ça vient de deux sources différentes.
    servers(state): GameServer[] {
      return state.staticServers.map(server => {
        const live = state.liveBySlug[server.slug]
        return live
          ? {
              ...server,
              online: live.online,
              numPlayers: live.numPlayers,
              maxPlayers: live.maxPlayers,
              checkedAt: live.checkedAt,
            }
          : server
      })
    },

    hasOnlineServer(): boolean {
      return this.servers.some(server => server.online === true)
    },

    // Détail complet (joueurs, position, journal…) d'un serveur, pour le dashboard. Null tant
    // qu'aucun snapshot n'est encore arrivé, ou si ce jeu n'a pas de dashboard.
    liveDetailBySlug: state => (slug: string) => state.liveBySlug[slug]?.live ?? null,
  },

  actions: {
    async load() {
      this.loading = true
      this.error = null
      try {
        this.staticServers = await fetchGameServers()
      } catch {
        this.error = 'Impossible de charger les serveurs de jeux.'
      } finally {
        this.loading = false
      }
    },

    async ensureLoaded() {
      if (this.staticServers.length || this.loading) return
      await this.load()
    },

    // Cold-start (état actuel sans attendre le prochain tick de 10s) puis abonnement au push.
    // Appelé par plusieurs composants (widget, dashboard) qui partagent le même store : le garde
    // évite de refaire le cold-start ou de doubler l'abonnement à chaque montage.
    async ensureLiveSubscribed() {
      if (this.liveSubscribed) return
      this.liveSubscribed = true

      try {
        this.applySnapshots(await fetchGameServersLiveState())
      } catch {
        // Pas grave : le prochain push (10s max) rattrape l'état, pas la peine d'afficher une
        // erreur pour un simple retard d'affichage.
      }

      coreHubConnection.on<GameServerLiveSnapshot[]>(LIVE_UPDATED_EVENT, snapshots => this.applySnapshots(snapshots))
    },

    // À la déconnexion : l'abonnement au hub vit hors du state Pinia, $reset() seul ne le
    // couperait pas et en perdrait la référence (même raison que l'ancien stopAutoRefresh()).
    unsubscribeLive() {
      coreHubConnection.off(LIVE_UPDATED_EVENT)
    },

    applySnapshots(snapshots: GameServerLiveSnapshot[]) {
      for (const snapshot of snapshots) {
        this.liveBySlug[snapshot.slug] = snapshot
        if (snapshot.live?.log.length) this.appendLog(snapshot.slug, snapshot.live.log)
      }
    },

    appendLog(slug: string, lines: string[]) {
      if (!lines.length) return
      this.logs[slug] = [...(this.logs[slug] ?? []), ...lines].slice(-MAX_LOG_LINES)
    },

    clearLog(slug: string) {
      delete this.logs[slug]
    },
  },
})
