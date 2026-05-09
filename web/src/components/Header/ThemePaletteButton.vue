<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import {
  PICO_THEMES,
  type PicoTheme,
  getPicoTheme,
  setPicoTheme
} from '@/ui/picoTheme'

const open = ref(false)
const currentTheme = ref<PicoTheme>('azure')

onMounted(() => {
  currentTheme.value = getPicoTheme()
  document.addEventListener('click', close)
  window.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', close)
  window.removeEventListener('scroll', onScroll, true)
})

const toggle = () => {
  open.value = !open.value
}

const selectTheme = (theme: PicoTheme) => {
  currentTheme.value = theme
  setPicoTheme(theme)
}

const close = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (!target.closest('.palette-wrapper')) {
    open.value = false
  }
}

const onScroll = () => {
  if (open.value) {
    open.value = false
  }
}
</script>

<template>
  <div class="palette-wrapper">
    <button
      class="theme-button"
      aria-label="Changer la couleur"
      title="Couleur"
      @click="toggle"
    >
      <i class="mdi mdi-palette" aria-hidden="true"></i>
    </button>

    <div
      v-if="open"
      class="palette-popover"
      role="dialog"
    >
      <p class="palette-title">Couleur du thème</p>

      <div class="palette-grid">
        <button
          v-for="theme in PICO_THEMES"
          :key="theme"
          class="palette-dot"
          :class="{ selected: theme === currentTheme }"
          :title="theme"
          type="button"
          :style="{ backgroundColor: `var(--pico-color-${theme})` }"
          @click="selectTheme(theme)"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Wrapper */
.palette-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
}

/* Bouton principal */
.theme-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;

  height: 2.25rem;
  padding: 0 0.5rem;

  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  cursor: pointer;

  color: inherit;
}

.theme-button:hover {
  background: var(--pico-muted-background-color);
}

.theme-button i {
  font-size: 1.25rem;
}

/* Popover */
.palette-popover {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  z-index: 1000;

  padding: 0.75rem 1rem;
  min-width: 190px;

  background: var(--pico-modal-overlay-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);

  font-size: 0.85rem;
  color: var(--pico-color);
}

.palette-title {
  margin: 0 0 0.5rem;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--pico-muted-color);
}

/* Grille */
.palette-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, 32px);
  gap: 0.5rem;
}

/* Cercle */
.palette-dot {
  width: 32px;
  height: 32px;
  border-radius: 50%;

  border: 1px solid var(--pico-muted-border-color);
  cursor: pointer;
}

/* Thème sélectionné */
.palette-dot.selected {
  outline: 2px solid var(--pico-primary);
  outline-offset: 2px;
}
</style>
