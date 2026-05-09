<script setup lang="ts">
import { computed } from 'vue'
import { useRiotStore } from '@/modules/Riot/riot.store'
import { addToMySkins, removeFromMySkins, addToWatchlist, removeFromWatchlist } from '@/modules/Riot/valorant/fetch/valorantUserSkins.fetch'
import toast from '@/services/toast'

interface ValorantSkin {
  id: number
  assetId: string
  name: string
  iconUrl: string | null
  levels: Array<{ assetId: string; levelIndex: number; displayIconUrl: string | null }>
}

const props = defineProps<{ skin: ValorantSkin }>()

const emit = defineEmits<{ preview: [url: string, name: string] }>()

const riotStore = useRiotStore()

const displayIcon = computed(
  () => props.skin.iconUrl ?? props.skin.levels?.[0]?.displayIconUrl ?? null
)

const isOwned = computed(() => riotStore.isSkinOwned(props.skin.id))
const isWatched = computed(() => riotStore.isSkinWatched(props.skin.id))

function onImageClick() {
  if (displayIcon.value) emit('preview', displayIcon.value, props.skin.name)
}

async function toggleOwned() {
  const targetState = !isOwned.value
  try {
    if (targetState) {
      // If we mark as owned, we must ensure it's removed from watchlist
      if (isWatched.value) {
        await removeFromWatchlist(props.skin.id)
      }
      await addToMySkins(props.skin.id)
    } else {
      await removeFromMySkins(props.skin.id)
    }
    riotStore.toggleOwnedLocally(props.skin.id, targetState)
  } catch (e: any) {
    toast.error(e.message || 'Erreur lors de la mise à jour de la collection')
  }
}

async function toggleWatched() {
  if (isOwned.value) return

  const targetState = !isWatched.value
  try {
    if (targetState) {
      await addToWatchlist(props.skin.id)
      riotStore.addToWatchlistLocally(props.skin.id)
    } else {
      await removeFromWatchlist(props.skin.id)
      riotStore.removeFromWatchlistLocally(props.skin.id)
    }
  } catch (e: any) {
    toast.error(e.message || 'Erreur lors de la mise à jour de la watchlist')
  }
}
</script>

<template>
  <article class="skin-card">
    <div class="skin-image-wrap" @click="onImageClick">
      <img
        v-if="displayIcon"
        :src="displayIcon"
        :alt="skin.name"
        class="skin-image"
        loading="lazy"
      />
      <div v-else class="skin-image-placeholder">
        <i class="mdi mdi-image-off-outline" />
      </div>

      <div class="card-actions">
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
    </div>

    <div class="skin-name">{{ skin.name }}</div>
  </article>
</template>

<style lang="scss" scoped>
.skin-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  cursor: pointer;
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  animation: card-enter 0.4s cubic-bezier(0.22, 1, 0.36, 1) both;

  &:hover {
    transform: translateY(-5px);
    border-color: color-mix(in srgb, var(--pico-primary) 45%, transparent);
    box-shadow:
      0 12px 32px rgba(0, 0, 0, 0.2),
      0 0 0 1px color-mix(in srgb, var(--pico-primary) 18%, transparent);

    .skin-image { transform: scale(1.06); }

    .action-btn { opacity: 1; }
  }
}

@keyframes card-enter {
  from { opacity: 0; transform: translateY(16px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* ── Image ───────────────────────────────────────────────────────────────── */
.skin-image-wrap {
  position: relative;
  background: #0d0d1a;
  aspect-ratio: 1 / 1;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 1rem;
}

.skin-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  transition: transform 0.28s ease;
}

.skin-image-placeholder {
  font-size: 2rem;
  color: var(--pico-muted-color);
  opacity: 0.4;
}

/* ── Actions overlay ─────────────────────────────────────────────────────── */
.card-actions {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
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
  opacity: 0;
  transition: opacity 0.2s ease, color 0.15s ease, background 0.15s ease;

  i { font-size: 0.95rem; }

  &:hover {
    background: rgba(0, 0, 0, 0.75);
    color: #fff;
  }

  &.active {
    opacity: 1;
  }

  &.active i { color: #2ecc71; }

  &:nth-child(2).active i { color: var(--pico-primary); }
}

/* ── Name ────────────────────────────────────────────────────────────────── */
.skin-name {
  padding: 0.6rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--pico-color);
  line-height: 1.3;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
