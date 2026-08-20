<script setup lang="ts">
import { computed } from 'vue'
import type { GameServer } from '../types/gameServers.types'
import { formatRelativeTime } from '@/utils/formatRelativeTime'
import { useClipboard } from '@/composables/useClipboard'

const props = defineProps<{
  server: GameServer
}>()

const { copy } = useClipboard()

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
</style>
