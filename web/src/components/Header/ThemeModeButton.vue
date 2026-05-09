<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { getTheme, setTheme } from '@/ui/theme'

type ThemeMode = 'auto' | 'light' | 'dark'

const theme = ref<ThemeMode>('auto')

onMounted(() => {
  theme.value = getTheme() as ThemeMode
})

const cycleTheme = () => {
  const next: Record<ThemeMode, ThemeMode> = {
    auto: 'light',
    light: 'dark',
    dark: 'auto'
  }

  theme.value = next[theme.value]
  setTheme(theme.value)
}

const themeIcon = computed(() => {
  switch (theme.value) {
    case 'light': return 'mdi-weather-sunny'
    case 'dark': return 'mdi-weather-night'
    default: return 'mdi-theme-light-dark'
  }
})

const themeLabel = computed(() => {
  switch (theme.value) {
    case 'light': return 'Light'
    case 'dark': return 'Dark'
    default: return 'Auto'
  }
})
</script>

<template>
  <button
    class="theme-button"
    :title="`Thème : ${themeLabel}`"
    aria-label="Changer le thème"
    @click="cycleTheme"
  >
    <i class="mdi" :class="themeIcon" aria-hidden="true"></i>
    <span class="theme-label">{{ themeLabel }}</span>
  </button>
</template>

<style scoped>
.theme-button {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;

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
  line-height: 1;
}

.theme-label {
  display: none;
  font-size: 0.85rem;
  letter-spacing: 0.04em;
}

@media (min-width: 768px) {
  .theme-label {
    display: inline;
  }
}
</style>
