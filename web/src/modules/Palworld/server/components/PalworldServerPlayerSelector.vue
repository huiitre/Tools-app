<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useFloating, offset, flip, shift, autoUpdate } from '@floating-ui/vue'
import { usePalworldServerDataStore } from '../serverData.store'

const serverDataStore = usePalworldServerDataStore()

const isOpen = ref(false)
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)

const selectedPlayerLabel = computed(() => {
  const selectedPlayerId = serverDataStore.selectedPlayerIds[0]
  if (!selectedPlayerId) return 'Joueur Palworld'

  return serverDataStore.guilds
    .flatMap(guild => guild.players)
    .find(player => player.playerUid === selectedPlayerId)?.name ?? 'Joueur Palworld'
})

const lastSyncedLabel = computed(() => serverDataStore.lastSyncedAt
  ? new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' })
    .format(new Date(serverDataStore.lastSyncedAt))
  : 'Jamais synchronisé')

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-start',
  middleware: [offset(10), flip(), shift()],
  whileElementsMounted: autoUpdate,
})

function selectPlayer(playerId: string) {
  serverDataStore.setSelectedPlayerIds([playerId])
  isOpen.value = false
}

function clearSelection() {
  serverDataStore.clearSelectedPlayers()
  isOpen.value = false
}

function togglePopup() {
  isOpen.value = !isOpen.value
}

function onClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (
    isOpen.value
    && reference.value
    && floating.value
    && !reference.value.contains(target)
    && !floating.value.contains(target)
  ) {
    isOpen.value = false
  }
}

function onScroll(event: Event) {
  if (isOpen.value && !floating.value?.contains(event.target as Node)) {
    isOpen.value = false
  }
}

onMounted(() => {
  window.addEventListener('mousedown', onClickOutside)
  window.addEventListener('scroll', onScroll, { capture: true })
})

onBeforeUnmount(() => {
  window.removeEventListener('mousedown', onClickOutside)
  window.removeEventListener('scroll', onScroll, { capture: true })
})
</script>

<template>
  <div class="palworld-server-player-selector">
    <button
      ref="reference"
      type="button"
      class="palworld-server-player-selector__trigger"
      :class="{ active: serverDataStore.selectedPlayerIds.length > 0 }"
      :disabled="serverDataStore.loading || !!serverDataStore.error"
      @click="togglePopup"
    >
      <i class="mdi mdi-account-group-outline" aria-hidden="true" />
      <span>{{ selectedPlayerLabel }}</span>
      <i class="mdi" :class="isOpen ? 'mdi-chevron-up' : 'mdi-chevron-down'" aria-hidden="true" />
    </button>

    <Teleport to="body">
      <Transition name="pop">
        <div
          v-if="isOpen"
          ref="floating"
          :style="floatingStyles"
          class="floating-panel"
        >
          <header class="panel-header">
            <div>
              <strong>Inventaire Palworld</strong>
              <small>Synchronisé le {{ lastSyncedLabel }}</small>
            </div>
            <button type="button" class="clear-selection-button" @click="clearSelection">
              Aucun Pal
            </button>
          </header>

          <div class="popup-content custom-scrollbar">
            <p v-if="serverDataStore.loading" class="panel-status">Chargement de l’inventaire…</p>
            <p v-else-if="serverDataStore.error" class="panel-status">{{ serverDataStore.error }}</p>

            <div v-else class="guild-list">
              <section v-for="guild in serverDataStore.guilds" :key="guild.guildId" class="guild-section">
                <h3>{{ guild.name }}</h3>
                <button
                  v-for="player in guild.players"
                  :key="player.playerUid"
                  type="button"
                  class="player-option"
                  :class="{ active: serverDataStore.selectedPlayerIds.includes(player.playerUid) }"
                  @click="selectPlayer(player.playerUid)"
                >
                  <span>{{ player.name }}</span>
                  <small>{{ serverDataStore.playerPalCount(player.playerUid) }} Pals</small>
                  <i v-if="serverDataStore.selectedPlayerIds.includes(player.playerUid)" class="mdi mdi-check" aria-hidden="true" />
                </button>
              </section>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped lang="scss">
.palworld-server-player-selector {
  display: inline-flex;
}

.palworld-server-player-selector__trigger {
  display: inline-flex;
  align-items: center;
  gap: .4rem;
  height: 2rem;
  width: auto;
  margin: 0;
  padding: 0 .55rem;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: .75rem;

  &.active {
    border-color: var(--pico-muted-border-color);
    background: var(--pico-card-background-color);
    color: var(--pico-color);
  }
}

.floating-panel {
  position: absolute;
  z-index: 1000;
  width: 280px;
  max-height: min(480px, calc(100vh - 130px));
  overflow: hidden;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  box-shadow: 0 12px 32px rgb(0 0 0 / 40%), 0 0 0 1px rgb(255 255 255 / 5%);
  color: var(--pico-color);
  display: flex;
  flex-direction: column;
}

.popup-content {
  flex: 1;
  overflow-y: auto;
  padding: .75rem;
  display: flex;
  flex-direction: column;
  gap: .65rem;
}

.panel-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: .5rem;
  padding: .75rem;
  background: rgb(255 255 255 / 3%);
  border-bottom: 1px solid var(--pico-card-border-color);

  strong,
  small {
    display: block;
  }

  strong {
    font-size: .78rem;
  }

  small {
    margin-top: .15rem;
    color: var(--pico-muted-color);
    font-size: .65rem;
  }
}

.clear-selection-button {
  width: auto;
  margin: 0;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: .65rem;
}

.panel-status {
  margin: 0;
  color: var(--pico-muted-color);
  font-size: .72rem;
}

.guild-list {
  display: flex;
  flex-direction: column;
  gap: .7rem;
}

.pop-enter-active,
.pop-leave-active {
  transition: transform .2s cubic-bezier(.34, 1.56, .64, 1), opacity .2s ease;
}

.pop-enter-from,
.pop-leave-to {
  opacity: 0;
  transform: scale(.95) translateY(-5px);
}

.custom-scrollbar {
  &::-webkit-scrollbar { width: 4px; }
  &::-webkit-scrollbar-track { background: transparent; }
  &::-webkit-scrollbar-thumb {
    background: var(--pico-muted-border-color);
    border-radius: 10px;
  }
}

.guild-section {
  display: flex;
  flex-direction: column;
  gap: .25rem;

  h3 {
    margin: 0 0 .15rem;
    color: var(--pico-color);
    font-size: .68rem;
    font-weight: 600;
  }
}

.player-option {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: .5rem;
  width: 100%;
  margin: 0;
  padding: .45rem .5rem;
  border: 1px solid transparent;
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-color);
  font-size: .72rem;
  text-align: left;

  &:hover,
  &.active {
    border-color: var(--pico-muted-border-color);
    background: var(--pico-card-background-color);
  }

  &.active {
    border-color: var(--pico-primary);
    color: var(--pico-color);
    font-weight: 600;
  }

  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  small {
    color: var(--pico-muted-color);
    font-size: .62rem;
    white-space: nowrap;
  }
}
</style>
