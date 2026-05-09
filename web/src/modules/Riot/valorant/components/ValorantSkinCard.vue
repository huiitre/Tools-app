<script setup lang="ts">
import { computed } from 'vue'
import type { ValorantSkin } from '../valorant.types'
import ValorantSkinActions from './ValorantSkinActions.vue'

const props = defineProps<{ skin: ValorantSkin }>()

const emit = defineEmits<{ preview: [url: string, name: string] }>()

const displayIcon = computed(
  () => props.skin.iconUrl ?? props.skin.levels?.[0]?.displayIconUrl ?? null
)

function onImageClick() {
  if (displayIcon.value) emit('preview', displayIcon.value, props.skin.name)
}
</script>
...

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

      <ValorantSkinActions :skin-id="skin.id" class="card-actions" />
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

    :deep(.action-btn) { opacity: 1; }
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

  :deep(.action-btn) {
    opacity: 0;
    transition: opacity 0.2s ease, color 0.15s ease, background 0.15s ease;

    &.active {
      opacity: 1;
    }
  }
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

