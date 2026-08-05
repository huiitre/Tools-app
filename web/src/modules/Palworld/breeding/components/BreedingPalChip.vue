<script setup lang="ts">
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'

withDefaults(defineProps<{
  pal: PalworldPalListItem | null
  gender?: 'Male' | 'Female' | null
  size?: number
  clickable?: boolean
  layout?: 'horizontal' | 'vertical'
  showCatalogDetails?: boolean
}>(), {
  gender: null,
  size: 40,
  clickable: false,
  layout: 'horizontal',
  showCatalogDetails: false,
})

const emit = defineEmits<{
  click: []
}>()
</script>

<template>
  <span class="breeding-chip" :class="[layout, { clickable }]" @click="clickable && emit('click')">
    <span v-if="showCatalogDetails && pal?.paldexIndex != null" class="breeding-chip-number">
      #{{ pal.paldexIndex }}{{ pal.paldexSuffix ?? '' }}
    </span>
    <span class="breeding-chip-avatar" :style="{ width: `${size}px`, height: `${size}px` }">
      <img v-if="pal?.imageUrl" :src="pal.imageUrl" :alt="pal.name" :width="size" :height="size" loading="lazy">
      <i v-else class="mdi mdi-help" />
    </span>
    <span class="breeding-chip-details">
      <span class="breeding-chip-name">
        {{ pal?.name ?? '—' }}
        <i v-if="gender === 'Male'" class="mdi mdi-gender-male gender-male" />
        <i v-else-if="gender === 'Female'" class="mdi mdi-gender-female gender-female" />
      </span>
      <small v-if="showCatalogDetails && pal?.elements.length" class="breeding-chip-type">
        {{ pal.elements.map(element => element.name).join(' · ') }}
      </small>
    </span>
  </span>
</template>

<style lang="scss" scoped>
.breeding-chip {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;

  &.clickable {
    cursor: pointer;
  }

  &.vertical {
    flex-direction: column;
    justify-content: center;
    gap: 0.25rem;
    min-width: 0;
  }
}

.breeding-chip-number {
  position: absolute;
  top: -.58rem;
  left: -.42rem;
  padding: .08rem .25rem;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 999px;
  background: var(--pico-card-background-color);
  color: var(--pico-muted-color);
  font-size: .48rem;
  font-weight: 700;
  line-height: 1;
}

.breeding-chip-avatar {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  overflow: hidden;

  img { display: block; object-fit: cover; }

  i { color: var(--pico-muted-color); }
}

.breeding-chip-name {
  font-size: 0.82rem;
  color: var(--pico-color);
  white-space: nowrap;
}

.breeding-chip-details { display: flex; flex-direction: column; min-width: 0; }
.breeding-chip-type { overflow: hidden; color: var(--pico-muted-color); font-size: .55rem; line-height: 1.2; text-overflow: ellipsis; white-space: nowrap; }

.vertical .breeding-chip-name {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: center;
  font-size: 0.82rem;
}

.gender-male { color: #4a90d9; }
.gender-female { color: #d9598f; }
</style>
