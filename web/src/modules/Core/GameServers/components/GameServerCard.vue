<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { GAME_SERVER_REOPEN_COOLDOWN_MS, type GameServer } from '../types/gameServers.types'
import { requestGameServerReopen } from '../fetch/gameServers.fetch'
import { useGameServersStore } from '../store/gameServers.store'
import { formatRelativeTime } from '@/utils/formatRelativeTime'
import { formatBytes } from '@/utils/formatBytes'
import { useClipboard } from '@/composables/useClipboard'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

const props = defineProps<{
  server: GameServer
}>()

const emit = defineEmits<{ openDashboard: [], openMods: [] }>()

// Un serveur avec un modpack mais sans liste propose tout de même le téléchargement.
const hasMods = computed(() => props.server.modCount > 0 || props.server.hasModpack)

const modsLabel = computed(() =>
  props.server.modCount > 0 ? `${props.server.modCount} mod${props.server.modCount > 1 ? 's' : ''}` : 'Mods'
)

const saveTitle = computed(() => {
  const { saveSize, saveDate } = props.server
  const parts = [
    saveDate ? `Sauvegarde du ${new Date(saveDate).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' })}` : 'Dernière sauvegarde',
    saveSize ? formatBytes(saveSize) : null
  ]
  return parts.filter(Boolean).join(' · ')
})

const { copy } = useClipboard()

/* ── Demande de réouverture (serveur hors ligne) ─────────── */
const store = useGameServersStore()
const isOffline = computed(() => props.server.online === false)

// Le tableau de bord d'un serveur éteint n'a rien à montrer : sa place revient à la demande.
const showDashboard = computed(() => props.server.hasDashboard && !isOffline.value)

// Rafraîchi chaque minute pour que le bouton se réactive seul à la fin de l'heure.
const now = ref(Date.now())
let ticker: ReturnType<typeof setInterval> | null = null
onMounted(() => { ticker = setInterval(() => { now.value = Date.now() }, 60_000) })
onBeforeUnmount(() => { if (ticker) clearInterval(ticker) })

const nextReopenAt = computed(() => {
  const requestedAt = props.server.reopenRequestedAt
  return requestedAt ? new Date(requestedAt).getTime() + GAME_SERVER_REOPEN_COOLDOWN_MS : null
})
const reopenRequested = computed(() => nextReopenAt.value !== null && nextReopenAt.value > now.value)
const sendingReopen = ref(false)

const formatTime = (time: number | string) =>
  new Date(time).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })

const reopenTitle = computed(() =>
  reopenRequested.value
    ? `Demandée à ${formatTime(props.server.reopenRequestedAt!)} · nouvelle demande possible à ${formatTime(nextReopenAt.value!)}`
    : 'Demander la réouverture : prévient les administrateurs que ce serveur est hors ligne'
)

const requestReopen = async () => {
  if (reopenRequested.value || sendingReopen.value) return
  sendingReopen.value = true
  try {
    const result = await requestGameServerReopen(props.server.slug)
    store.markReopenRequested(props.server.slug, result.requestedAt)
    toast.success('Les administrateurs ont été prévenus')
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : "La demande n'a pas pu être envoyée")
  } finally {
    sendingReopen.value = false
  }
}

const statusClass = computed(() => {
  if (props.server.online === true) return 'online'
  if (props.server.online === false) return 'offline'
  return 'unknown'
})

const statusLabel = computed(() => {
  if (props.server.online === true) return 'En ligne'
  if (props.server.online === false) return 'Hors ligne'
  return 'Pas encore vérifié'
})

const checkedLabel = computed(() =>
  props.server.checkedAt ? formatRelativeTime(props.server.checkedAt) : 'Pas encore vérifié'
)

const playerCount = computed(() => {
  const { numPlayers, maxPlayers } = props.server
  if (numPlayers === null) return null
  return maxPlayers !== null ? `${numPlayers} / ${maxPlayers} joueurs` : `${numPlayers} joueurs`
})

const hostLabel = computed(() => props.server.clientHost ?? '???')
const portLabel = computed(() => props.server.clientPort !== null ? String(props.server.clientPort) : '??')
const canCopyConnection = computed(() => props.server.clientHost !== null && props.server.clientPort !== null)

const copyConnection = () => {
  if (!canCopyConnection.value) return
  copy(`${props.server.clientHost}:${props.server.clientPort}`, 'Adresse de connexion')
}
</script>

<template>
  <article class="game-server-card">
    <div class="game-server-picture">
      <img v-if="server.pictureUrl" :src="server.pictureUrl" :alt="server.gameName" />
      <i v-else class="mdi mdi-controller-classic" aria-hidden="true"></i>

      <span
        class="connection-badge"
        :class="{ copyable: canCopyConnection }"
        :title="canCopyConnection ? 'Copier l\'adresse de connexion' : undefined"
        @click="copyConnection"
      >{{ hostLabel }}<span class="connection-sep">:</span><span
        class="connection-port"
        :class="{ unknown: server.clientPort === null }"
      >{{ portLabel }}</span></span>

      <span class="status-badge" :class="statusClass" :title="statusLabel">
        <span class="status-badge-dot" />
        {{ statusLabel }}
      </span>
    </div>

    <div class="game-server-content">
      <h4 class="game-server-name">{{ server.gameName }}</h4>
      <p class="game-server-server-name">{{ server.serverName }}</p>

      <div class="game-server-footer">
        <span class="game-server-status" :class="statusClass">{{ statusLabel }}</span>
        <span v-if="playerCount" class="game-server-players">{{ playerCount }}</span>
      </div>

      <p class="game-server-checked">{{ checkedLabel }}</p>
    </div>

    <div v-if="showDashboard || hasMods || server.saveUrl || isOffline" class="game-server-actions">
      <!-- Absente tant qu'aucun provider ne sait alimenter un dashboard, et quand le serveur est éteint. -->
      <button
        v-if="showDashboard"
        type="button"
        class="game-server-action"
        @click="emit('openDashboard')"
      >
        <i class="mdi mdi-view-dashboard-outline" aria-hidden="true" />
        Dashboard
      </button>

      <!-- Absente tant que le manifest du serveur ne déclare ni liste de mods ni modpack. -->
      <button
        v-if="hasMods"
        type="button"
        class="game-server-action"
        @click="emit('openMods')"
      >
        <i class="mdi mdi-puzzle-outline" aria-hidden="true" />
        {{ modsLabel }}
      </button>

      <!-- Lien direct vers les assets : le fichier ne transite jamais par l'API. -->
      <a
        v-if="server.saveUrl"
        class="game-server-action"
        :href="server.saveUrl"
        :title="saveTitle"
        download
      >
        <i class="mdi mdi-download" aria-hidden="true" />
        Save
      </a>

      <!-- Seulement hors ligne : prévient les administrateurs, une fois par heure et par serveur. -->
      <button
        v-if="isOffline"
        type="button"
        class="game-server-action game-server-action--reopen"
        :class="{ requested: reopenRequested }"
        :disabled="reopenRequested || sendingReopen"
        :aria-busy="sendingReopen"
        :title="reopenTitle"
        @click="requestReopen"
      >
        {{ reopenRequested ? 'Demandée' : 'Demande de réouverture' }}
      </button>
    </div>
  </article>
</template>

<style scoped lang="scss">
.game-server-card {
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  background: var(--pico-card-background-color);
}

.game-server-picture {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  background: var(--pico-muted-background-color);
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .mdi {
    font-size: 3rem;
    opacity: 0.35;
  }
}

.status-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;

  display: inline-flex;
  align-items: center;
  gap: 0.35rem;

  padding: 0.3rem 0.6rem;
  border-radius: 999px;

  background: rgba(20, 20, 20, 0.72);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);

  font-size: 0.72rem;
  font-weight: 700;
  color: #fff;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
}

.status-badge-dot {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 50%;
  flex-shrink: 0;
  box-shadow: 0 0 6px 1px currentColor;
}

.status-badge.online .status-badge-dot { background-color: var(--pico-color-green-550); color: var(--pico-color-green-550); }
.status-badge.offline .status-badge-dot { background-color: var(--pico-color-red-550); color: var(--pico-color-red-550); }
.status-badge.unknown .status-badge-dot { background-color: #bbb; color: #bbb; }

.connection-badge {
  position: absolute;
  top: 0.5rem;
  left: 0.5rem;

  padding: 0.3rem 0.6rem;
  border-radius: 999px;

  background: rgba(20, 20, 20, 0.72);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);

  font-family: var(--pico-font-family-monospace, monospace);
  font-size: 0.7rem;
  font-weight: 700;
  color: #fff;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);

  .connection-sep {
    opacity: 0.6;
  }

  .connection-port.unknown {
    color: var(--pico-color-red-550);
  }
}

.connection-badge.copyable {
  cursor: pointer;

  &:hover {
    background: rgba(20, 20, 20, 0.88);
  }
}

.game-server-content {
  padding: 0.85rem 1rem 1rem;
}

.game-server-name {
  margin: 0 0 0.15rem;
  font-size: 1rem;
  font-weight: 600;
}

.game-server-server-name {
  margin: 0 0 0.6rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}

.game-server-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.game-server-status {
  font-size: 0.8rem;
  font-weight: 600;

  &.online { color: var(--pico-color-green-550); }
  &.offline { color: var(--pico-color-red-550); }
  &.unknown { color: var(--pico-muted-color); }
}

.game-server-players {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.game-server-checked {
  margin: 0.4rem 0 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  opacity: 0.7;
}

.game-server-actions {
  display: flex;
  border-top: 1px solid var(--pico-muted-border-color);
}

.game-server-action {
  flex: 1;
  margin: 0;
  padding: 0.5rem;
  border: none;
  border-radius: 0;

  & + & {
    border-left: 1px solid var(--pico-muted-border-color);
  }

  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;

  background: var(--pico-muted-background-color);
  color: inherit;
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
  text-decoration: none;
  cursor: pointer;

  &:hover {
    background: var(--pico-primary-background);
    color: var(--pico-primary-inverse);
  }
}

// Teinté de l'accent pour se distinguer des autres actions, sans crier.
.game-server-action--reopen {
  background: color-mix(in srgb, var(--pico-primary) 14%, transparent);
  color: var(--pico-primary);
  // Libellé long : il passe sur deux lignes plutôt que d'être coupé quand la carte a trois boutons.
  white-space: normal;
  line-height: 1.2;
  text-align: center;

  &.requested,
  &:disabled {
    background: color-mix(in srgb, var(--pico-primary) 5%, transparent);
    color: var(--pico-muted-color);
    cursor: default;
    opacity: 1;
  }

  &:disabled:hover {
    background: color-mix(in srgb, var(--pico-primary) 5%, transparent);
    color: var(--pico-muted-color);
  }
}
</style>
