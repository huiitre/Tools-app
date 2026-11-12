<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'

// Bouton de rafraîchissement et intervalle d'auto-refresh en secondes (0 = désactivé), mémorisé par tableau.
const props = defineProps<{ storageKey: string }>()
const emit = defineEmits<{ refresh: [silent: boolean] }>()

function readSeconds(): number {
  try {
    const raw = Number(localStorage.getItem(props.storageKey))
    return Number.isFinite(raw) && raw > 0 ? Math.floor(raw) : 0
  } catch {
    return 0
  }
}

const seconds = ref(readSeconds())
let timer: ReturnType<typeof setInterval> | null = null

function onInput(event: Event) {
  const value = Math.floor(Number((event.target as HTMLInputElement).value))
  seconds.value = Number.isFinite(value) && value > 0 ? value : 0
}

watch(seconds, value => {
  try { localStorage.setItem(props.storageKey, String(value)) } catch { /* préférence non mémorisée */ }
  if (timer) clearInterval(timer)
  timer = value > 0 ? setInterval(() => emit('refresh', true), value * 1000) : null
}, { immediate: true })

onBeforeUnmount(() => { if (timer) clearInterval(timer) })
</script>

<template>
  <div class="auto-refresh">
    <button type="button" class="icon" title="Rafraîchir" @click="emit('refresh', false)">
      <i class="mdi mdi-refresh" />
    </button>
    <input
      type="number"
      min="0"
      step="1"
      :value="seconds"
      title="Rafraîchissement automatique, en secondes (0 = désactivé)"
      aria-label="Rafraîchissement automatique, en secondes"
      @change="onInput"
    />
    <span>s</span>
  </div>
</template>

<style scoped lang="scss">
.auto-refresh {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

button.icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: auto;
  margin: 0;
  padding: 0.25rem;
  background: none;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;
  i { font-size: 1.1rem; }
  &:hover { color: var(--pico-primary); }
}

input {
  width: 4rem;
  height: 2rem;
  margin: 0;
  padding: 0 0.4rem;
  font-size: 0.75rem;
}
</style>
