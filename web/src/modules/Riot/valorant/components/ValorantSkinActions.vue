<script setup lang="ts">
import { computed } from 'vue'
import { useRiotStore } from '@/modules/Riot/riot.store'
import { addToMySkins, removeFromMySkins, addToWatchlist, removeFromWatchlist } from '@/modules/Riot/valorant/fetch/valorantUserSkins.fetch'
import toast from '@/services/toast'

const props = defineProps<{ skinId: number }>()

const riotStore = useRiotStore()

const isOwned = computed(() => riotStore.isSkinOwned(props.skinId))
const isWatched = computed(() => riotStore.isSkinWatched(props.skinId))

async function toggleOwned() {
  const targetState = !isOwned.value
  try {
    if (targetState) {
      if (isWatched.value) {
        await removeFromWatchlist(props.skinId)
        riotStore.removeFromWatchlistLocally(props.skinId)
      }
      await addToMySkins(props.skinId)
    } else {
      await removeFromMySkins(props.skinId)
    }
    riotStore.toggleOwnedLocally(props.skinId, targetState)
  } catch (e: any) {
    toast.error(e.message || 'Erreur lors de la mise à jour de la collection')
  }
}

async function toggleWatched() {
  if (isOwned.value) return

  const targetState = !isWatched.value
  try {
    if (targetState) {
      await addToWatchlist(props.skinId)
      riotStore.addToWatchlistLocally(props.skinId)
    } else {
      await removeFromWatchlist(props.skinId)
      riotStore.removeFromWatchlistLocally(props.skinId)
    }
  } catch (e: any) {
    toast.error(e.message || 'Erreur lors de la mise à jour de la watchlist')
  }
}
</script>

<template>
  <div class="skin-actions">
    <button
      class="action-btn"
      :class="{ active: isOwned }"
      title="Je possède ce skin"
      @click.stop="toggleOwned"
    >
      <i :class="isOwned ? 'mdi mdi-check-circle' : 'mdi mdi-check-circle-outline'" />
    </button>
    <button
      v-if="!isOwned"
      class="action-btn"
      :class="{ active: isWatched }"
      title="Surveiller ce skin"
      @click.stop="toggleWatched"
    >
      <i :class="isWatched ? 'mdi mdi-bell' : 'mdi mdi-bell-outline'" />
    </button>
  </div>
</template>

<style lang="scss" scoped>
.skin-actions {
  display: flex;
  gap: 0.3rem;
}

.action-btn {
  width: 1.6rem;
  height: 1.6rem;
  border-radius: 50%;
  border: none;
  padding: 0;
  margin: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.55);
  color: rgba(255, 255, 255, 0.6);
  cursor: pointer;
  transition: opacity 0.2s ease, color 0.15s ease, background 0.15s ease;

  i { font-size: 0.95rem; }

  &:hover {
    background: rgba(0, 0, 0, 0.75);
    color: #fff;
  }

  &.active i { color: #2ecc71; }
  &:nth-child(2).active i { color: var(--pico-primary); }
}
</style>
