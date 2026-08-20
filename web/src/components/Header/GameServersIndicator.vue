<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useGameServersStore } from '@/modules/Core/GameServers/store/gameServers.store'
import type { GameServer } from '@/modules/Core/GameServers/types/gameServers.types'
import { formatRelativeTime } from '@/utils/formatRelativeTime'
import { useClipboard } from '@/composables/useClipboard'

const store = useGameServersStore()
const { copy } = useClipboard()
const open = ref(false)
const popoverRef = ref<HTMLElement | null>(null)

onMounted(() => {
  store.ensureLoaded()
  store.startAutoRefresh()
  document.addEventListener('click', close)
  window.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', close)
  window.removeEventListener('scroll', onScroll, true)
})

const onlineServers = computed(() => store.servers.filter(server => server.online === true))

const toggle = () => { open.value = !open.value }

const close = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (!target.closest('.game-servers-wrapper')) open.value = false
}

const onScroll = (e: Event) => {
  if (open.value && !popoverRef.value?.contains(e.target as Node)) open.value = false
}

const playerCount = (numPlayers: number | null, maxPlayers: number | null) => {
  if (numPlayers === null) return null
  return maxPlayers !== null ? `${numPlayers} / ${maxPlayers} joueurs` : `${numPlayers} joueurs`
}

const hostLabel = (server: GameServer) => server.clientHost ?? '???'
const portLabel = (server: GameServer) => server.clientPort !== null ? String(server.clientPort) : '??'
const canCopyConnection = (server: GameServer) => server.clientHost !== null && server.clientPort !== null

const copyConnection = (server: GameServer) => {
  if (!canCopyConnection(server)) return
  copy(`${server.clientHost}:${server.clientPort}`, 'Adresse de connexion')
}
</script>

<template>
  <div v-if="onlineServers.length" class="game-servers-wrapper">
    <button
      class="game-servers-trigger"
      aria-label="Serveurs de jeux en ligne"
      title="Serveurs de jeux en ligne"
      @click="toggle"
    >
      <i class="mdi mdi-controller-classic" aria-hidden="true"></i>
      <span class="game-servers-badge">{{ onlineServers.length }}</span>
    </button>

    <Transition name="notif-popover">
      <div v-if="open" ref="popoverRef" class="game-servers-popover" role="dialog" aria-label="Serveurs de jeux en ligne">
        <div class="game-servers-header">
          <span class="game-servers-header-title">Serveurs en ligne</span>
        </div>

        <ul class="game-servers-list" role="list">
          <li v-for="server in onlineServers" :key="`${server.gameName}-${server.serverName}`" class="game-servers-item">
            <div class="game-servers-item-icon">
              <img v-if="server.pictureUrl" :src="server.pictureUrl" :alt="server.gameName" />
              <i v-else class="mdi mdi-controller-classic" aria-hidden="true"></i>
            </div>
            <div class="game-servers-item-content">
              <p class="game-servers-item-title">{{ server.gameName }}</p>
              <p class="game-servers-item-subtitle">{{ server.serverName }}</p>
              <p
                class="game-servers-item-connection"
                :class="{ copyable: canCopyConnection(server) }"
                :title="canCopyConnection(server) ? 'Copier l\'adresse de connexion' : undefined"
                @click.stop="copyConnection(server)"
              >{{ hostLabel(server) }}<span class="connection-sep">:</span><span
                class="connection-port"
                :class="{ unknown: server.clientPort === null }"
              >{{ portLabel(server) }}</span></p>
            </div>
            <div class="game-servers-item-meta">
              <span v-if="playerCount(server.numPlayers, server.maxPlayers)" class="game-servers-item-players">
                {{ playerCount(server.numPlayers, server.maxPlayers) }}
              </span>
              <span v-if="server.checkedAt" class="game-servers-item-time">{{ formatRelativeTime(server.checkedAt) }}</span>
            </div>
          </li>
        </ul>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.game-servers-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
}

.game-servers-trigger {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  height: 2.25rem;
  padding: 0 0.5rem;

  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  cursor: pointer;
  color: inherit;

  &:hover { background: var(--pico-muted-background-color); }
  .mdi { font-size: 1.25rem; }
}

.game-servers-badge {
  position: absolute;
  top: -5px;
  right: -5px;

  min-width: 17px;
  height: 17px;
  padding: 0 3px;

  display: flex;
  align-items: center;
  justify-content: center;

  border-radius: 999px;
  background: var(--pico-color-green-550);
  color: #fff;
  font-size: 0.6rem;
  font-weight: 700;
  line-height: 1;
  pointer-events: none;
}

.game-servers-popover {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  z-index: 1100;

  width: 340px;
  max-height: 420px;
  overflow: hidden;
  display: flex;
  flex-direction: column;

  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);
}

.notif-popover-enter-active,
.notif-popover-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.notif-popover-enter-from,
.notif-popover-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.98);
}

.game-servers-header {
  padding: 0.65rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-shrink: 0;
}

.game-servers-header-title {
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--pico-muted-color);
}

.game-servers-list {
  overflow-y: auto;
  flex: 1;
  margin: 0;
  padding: 0;
  list-style: none;
}

.game-servers-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  padding: 0.65rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  transition: background 0.12s ease;

  &:last-child { border-bottom: none; }
  &:hover { background: var(--pico-muted-background-color); }
}

.game-servers-item-icon {
  flex-shrink: 0;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--pico-muted-background-color);

  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  .mdi { font-size: 1.1rem; opacity: 0.5; }
}

.game-servers-item-content {
  flex: 1;
  min-width: 0;
}

.game-servers-item-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--pico-color);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.game-servers-item-subtitle {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.game-servers-item-connection {
  margin: 0.2rem 0 0;
  font-family: var(--pico-font-family-monospace, monospace);
  font-size: 0.68rem;
  color: var(--pico-muted-color);
  opacity: 0.7;

  .connection-sep {
    opacity: 0.6;
  }

  .connection-port.unknown {
    color: var(--pico-color-red-550);
    font-weight: 700;
    opacity: 1;
  }
}

.game-servers-item-connection.copyable {
  cursor: pointer;

  &:hover {
    color: var(--pico-primary);
    opacity: 1;
  }
}

.game-servers-item-meta {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.15rem;
}

.game-servers-item-players {
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--pico-color-green-550);
}

.game-servers-item-time {
  font-size: 0.68rem;
  color: var(--pico-muted-color);
  opacity: 0.7;
}
</style>
