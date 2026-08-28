<script setup lang="ts">
import { ref, watch } from 'vue'

const STORAGE_KEY = 'gameServers.map.sidebarOpen'
const open = ref(localStorage.getItem(STORAGE_KEY) !== 'false')

watch(open, value => localStorage.setItem(STORAGE_KEY, String(value)))
</script>

<template>
  <div class="map-sidebar" :class="{ 'map-sidebar--collapsed': !open }">
    <div class="map-sidebar-panel">
      <div v-if="open" class="map-sidebar-content">
        <slot />
      </div>
    </div>

    <button
      type="button"
      class="map-sidebar-toggle"
      :aria-expanded="open"
      :title="open ? 'Masquer la colonne' : 'Afficher la colonne'"
      @click="open = !open"
    >
      <i class="mdi" :class="open ? 'mdi-chevron-left' : 'mdi-chevron-right'" />
    </button>
  </div>
</template>

<style lang="scss" scoped>
.map-sidebar {
  display: flex;
  align-items: stretch;
  flex: 0 0 auto;
}

.map-sidebar-panel {
  width: 260px;
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
  border-right: none;
  border-radius: var(--pico-border-radius) 0 0 var(--pico-border-radius);
  background: var(--pico-card-background-color);
  transition: width 0.2s ease, border-color 0.2s ease, background-color 0.2s ease;
}

.map-sidebar--collapsed .map-sidebar-panel {
  width: 0;
  border-color: transparent;
}

.map-sidebar-toggle {
  flex: 0 0 auto;
  display: grid;
  place-items: center;
  width: 1.6rem;
  margin: 0;
  padding: 0;
  border: 1px solid var(--pico-card-border-color);
  border-left: 1px solid color-mix(in srgb, var(--pico-card-border-color) 45%, transparent);
  border-radius: 0 var(--pico-border-radius) var(--pico-border-radius) 0;
  background: var(--pico-card-background-color);
  color: var(--pico-muted-color);
  cursor: pointer;
}

.map-sidebar--collapsed .map-sidebar-toggle {
  border-radius: var(--pico-border-radius);
}

.map-sidebar-toggle:hover {
  color: var(--pico-primary);
  background: color-mix(in srgb, var(--pico-primary) 6%, var(--pico-card-background-color));
}

.map-sidebar-content {
  height: 100%;
  width: 260px;
  padding: 1rem;
  overflow-y: auto;
}

@media (max-width: 720px) {
  .map-sidebar {
    flex-direction: column;
  }

  .map-sidebar-panel {
    width: 100%;
    border: 1px solid var(--pico-card-border-color);
    border-top: none;
    border-radius: 0 0 var(--pico-border-radius) var(--pico-border-radius);
  }

  .map-sidebar--collapsed .map-sidebar-panel {
    width: 100%;
    border-color: transparent;
  }

  .map-sidebar-content {
    width: auto;
  }

  .map-sidebar-toggle {
    width: 100%;
    height: 1.6rem;
    border: 1px solid var(--pico-card-border-color);
    border-bottom: 1px solid color-mix(in srgb, var(--pico-card-border-color) 45%, transparent);
    border-radius: var(--pico-border-radius) var(--pico-border-radius) 0 0;
  }

  .map-sidebar--collapsed .map-sidebar-toggle {
    border-radius: var(--pico-border-radius);
  }
}
</style>
