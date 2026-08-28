<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useGameServersStore } from '../store/gameServers.store'
import GameServerCard from './GameServerCard.vue'
import GameServerDashboardModal from './GameServerDashboardModal.vue'
import type { GameServer } from '../types/gameServers.types'

const HIDE_OFFLINE_STORAGE_KEY = 'gameServers.hideOffline'

const store = useGameServersStore()

const hideOffline = ref(localStorage.getItem(HIDE_OFFLINE_STORAGE_KEY) === 'true')

watch(hideOffline, value => {
  localStorage.setItem(HIDE_OFFLINE_STORAGE_KEY, String(value))
})

onMounted(() => {
  store.ensureLoaded()
  store.startAutoRefresh()
})

const dashboardServer = ref<GameServer | null>(null)

const visibleServers = computed(() =>
  hideOffline.value ? store.servers.filter(server => server.online === true) : store.servers
)
</script>

<template>
  <section v-if="store.servers.length" class="game-servers-widget">
    <div class="game-servers-header">
      <h4 class="game-servers-title">Serveurs de jeux</h4>

      <label class="game-servers-filter">
        <span>Masquer les serveurs hors ligne</span>
        <input type="checkbox" role="switch" v-model="hideOffline" />
      </label>
    </div>

    <div v-if="visibleServers.length" class="game-servers-grid">
      <GameServerCard
        v-for="server in visibleServers"
        :key="server.slug"
        :server="server"
        @open-dashboard="dashboardServer = server"
      />
    </div>
    <p v-else class="game-servers-empty">Aucun serveur en ligne pour le moment.</p>

    <GameServerDashboardModal
      v-if="dashboardServer"
      :server="dashboardServer"
      @close="dashboardServer = null"
    />
  </section>
</template>

<style scoped lang="scss">
.game-servers-widget {
  width: 100%;
  max-width: 1100px;
  padding: 0 1rem;
  margin-bottom: 2rem;
}

.game-servers-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.game-servers-title {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--pico-muted-color);
}

.game-servers-filter {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0;
  cursor: pointer;

  span {
    font-size: 0.8rem;
    color: var(--pico-muted-color);
  }

  input[type='checkbox'] {
    margin: 0;
  }
}

.game-servers-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
}

@media (min-width: 640px) {
  .game-servers-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 1024px) {
  .game-servers-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.game-servers-empty {
  margin: 0;
  padding: 1.5rem 0;
  text-align: center;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}
</style>
