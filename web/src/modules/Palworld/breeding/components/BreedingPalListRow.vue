<script setup lang="ts">
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'

withDefaults(defineProps<{
  pal: PalworldPalListItem
  clickable?: boolean
  removable?: boolean
}>(), {
  clickable: false,
  removable: false,
})

const emit = defineEmits<{
  click: []
  remove: []
}>()
</script>

<template>
  <div class="pal-row" :class="{ clickable }" @click="clickable && emit('click')">
    <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="28" height="28" loading="lazy">
    <i v-else class="mdi mdi-help pal-row-noimg" />

    <span class="pal-row-name">{{ pal.name }}</span>

    <span v-if="pal.elements.length" class="pal-row-elements">
      <span
        v-for="element in pal.elements"
        :key="element.id"
        class="element-icon-crop"
        :title="element.name"
        :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
      />
    </span>

    <i v-if="removable" class="mdi mdi-close pal-row-remove" @click.stop="emit('remove')" />
  </div>
</template>

<style lang="scss" scoped>
.pal-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.5rem;
  border-radius: 6px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);

  &.clickable {
    cursor: pointer;

    &:hover { border-color: var(--pico-primary); }
  }

  img {
    border-radius: 4px;
    flex-shrink: 0;
    object-fit: cover;
  }
}

.pal-row-noimg {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--pico-muted-color);
}

.pal-row-name {
  font-size: 0.8rem;
  color: var(--pico-color);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pal-row-elements {
  display: flex;
  gap: 0.2rem;
  flex-shrink: 0;
}

.element-icon-crop {
  display: inline-block;
  width: 16px;
  height: 16px;
  border-radius: 3px;
  background-repeat: no-repeat;
  background-position: left center;
  background-size: auto 100%;
}

.pal-row-remove {
  cursor: pointer;
  color: var(--pico-muted-color);
  flex-shrink: 0;

  &:hover { color: #e53e3e; }
}
</style>
