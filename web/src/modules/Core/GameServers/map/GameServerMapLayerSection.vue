<script setup lang="ts">
import { ref } from 'vue'

export interface MapLayerItem {
  key: string
  label: string
  sublabel?: string
  color?: string
  children?: string[]
}

defineProps<{
  title: string
  icon: string
  items: MapLayerItem[]
  hiddenKeys: Set<string>
  emptyLabel: string
}>()

const emit = defineEmits<{ toggle: [key: string] }>()

const collapsed = ref(false)
</script>

<template>
  <section class="map-layer-section">
    <button
      type="button"
      class="map-layer-section-header"
      :aria-expanded="!collapsed"
      @click="collapsed = !collapsed"
    >
      <span class="map-layer-section-title">
        <i class="mdi" :class="icon" aria-hidden="true" /> {{ title }}
      </span>
      <span class="map-layer-section-header-right">
        <span class="map-layer-section-count">{{ items.length }}</span>
        <i class="mdi mdi-chevron-down map-layer-section-chevron" :class="{ 'map-layer-section-chevron--collapsed': collapsed }" aria-hidden="true" />
      </span>
    </button>

    <template v-if="!collapsed">
      <ul v-if="items.length" class="map-layer-list">
        <li v-for="item in items" :key="item.key" class="map-layer-item">
          <label>
            <input
              type="checkbox"
              :checked="!hiddenKeys.has(item.key)"
              @change="emit('toggle', item.key)"
            >
            <span v-if="item.color" class="map-layer-item-dot" :style="{ background: item.color }" />
            <span class="map-layer-item-label">{{ item.label }}</span>
            <span v-if="item.sublabel" class="map-layer-item-sublabel">{{ item.sublabel }}</span>
          </label>

          <ul v-if="item.children?.length" class="map-layer-item-children">
            <li v-for="name in item.children" :key="name">{{ name }}</li>
          </ul>
        </li>
      </ul>
      <p v-else class="map-layer-empty">{{ emptyLabel }}</p>
    </template>
  </section>
</template>

<style lang="scss" scoped>
.map-layer-section + .map-layer-section {
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--pico-card-border-color);
}

.map-layer-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  margin: 0 0 0.6rem;
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  cursor: pointer;
}

.map-layer-section-title {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--pico-muted-color);

  i { font-size: 0.9rem; }
}

.map-layer-section-header-right {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.map-layer-section-count {
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  background: color-mix(in srgb, var(--pico-muted-color) 15%, transparent);
  border-radius: 999px;
  padding: 0.05rem 0.5rem;
}

.map-layer-section-chevron {
  font-size: 1rem;
  color: var(--pico-muted-color);
  transition: transform 0.2s ease;
}

.map-layer-section-chevron--collapsed {
  transform: rotate(-90deg);
}

.map-layer-list {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.map-layer-item label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.8rem;
  color: var(--pico-color);
}

.map-layer-item label input[type="checkbox"] {
  width: 1rem;
  height: 1rem;
  margin: 0;
  align-self: center;
  flex-shrink: 0;
}

.map-layer-item-dot {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 2px;
  flex-shrink: 0;
}

.map-layer-item-label {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.map-layer-item-sublabel {
  flex-shrink: 0;
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.map-layer-item-children {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  margin: 0.3rem 0 0;
  padding: 0 0 0 1.5rem;
  list-style: none;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.map-layer-empty {
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}
</style>
