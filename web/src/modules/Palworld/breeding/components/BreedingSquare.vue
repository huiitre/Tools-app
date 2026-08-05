<script setup lang="ts">
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'

defineProps<{
  label: string
  pal: PalworldPalListItem | null
  clickable?: boolean
  loading?: boolean
  active?: boolean
}>()

const emit = defineEmits<{
  click: []
}>()
</script>

<template>
  <div class="breeding-square" :class="{ clickable, filled: !!pal, active }" @click="clickable && emit('click')">
    <span class="breeding-square-label">{{ label }}</span>

    <div class="breeding-square-body">
      <span v-if="loading" class="spinner" />
      <template v-else-if="pal">
        <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="72" height="72" loading="lazy">
        <span class="pal-name">{{ pal.name }}</span>
        <span v-if="pal.elements.length" class="pal-elements">
          <span
            v-for="element in pal.elements"
            :key="element.id"
            class="element-icon-crop"
            :title="element.name"
            :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
          />
        </span>
      </template>
      <span v-else class="placeholder">
        <i class="mdi mdi-plus" />
      </span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.breeding-square {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem;
  width: 140px;
  border-radius: 12px;
  background: var(--pico-card-background-color);
  border: 2px solid var(--pico-card-border-color);
  transition: border-color 0.15s ease;

  &.clickable {
    cursor: pointer;

    &:hover {
      border-color: var(--pico-primary);
    }
  }

  &.active {
    border-color: var(--pico-primary);
  }
}

.breeding-square-label {
  font-size: 0.72rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
}

.breeding-square-body {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  min-height: 100px;
  justify-content: center;
}

.pal-name {
  font-size: 0.85rem;
  text-align: center;
  color: var(--pico-color);
}

.pal-elements {
  display: flex;
  gap: 0.3rem;
  margin-top: 0.1rem;
}

.element-icon-crop {
  display: inline-block;
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  border-radius: 4px;
  background-repeat: no-repeat;
  background-position: left center;
  background-size: auto 100%;
}

.placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  border: 1px dashed var(--pico-muted-border-color);
  color: var(--pico-muted-color);

  i { font-size: 1.5rem; }
}

.spinner {
  width: 24px;
  height: 24px;
  border: 2px solid var(--pico-card-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
