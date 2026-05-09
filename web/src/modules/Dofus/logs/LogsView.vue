<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useEnv } from '@/composables/useEnv'

interface LogEntry {
  id: string
  timestamp: string
  level: 'info' | 'warn' | 'error' | 'debug'
  service: string
  message: string
  data: unknown
}

const { isElectron } = useEnv()

const logs = ref<LogEntry[]>([])
const filterLevel = ref('all')
const filterService = ref('all')
const searchQuery = ref('')

const LEVELS = ['all', 'debug', 'info', 'warn', 'error'] as const

const services = computed(() => {
  const s = new Set(logs.value.map((l: LogEntry) => l.service))
  return ['all', ...Array.from(s).sort()]
})

const filtered = computed(() => {
  return logs.value.filter((l: LogEntry) => {
    if (filterLevel.value !== 'all' && l.level !== filterLevel.value) return false
    if (filterService.value !== 'all' && l.service !== filterService.value) return false
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase()
      if (!l.message.toLowerCase().includes(q) && !l.service.toLowerCase().includes(q)) return false
    }
    return true
  }).slice().reverse()
})

const clearLogs = async () => {
  if (window.electron) {
    await window.electron.clearElectronLogs()
    logs.value = []
  }
}

const formatTime = (iso: string) => iso.substring(11, 23)

const levelClass = (level: string) => `log-level--${level}`

onMounted(async () => {
  if (!window.electron) return

  const existing = await window.electron.getElectronLogs()
  if (existing) logs.value = existing as LogEntry[]

  window.electron.onElectronLog((entry) => {
    logs.value.push(entry as LogEntry)
  })
})

onBeforeUnmount(() => {
  window.electron?.offElectronLog()
})
</script>

<template>
  <div class="logs-view">
    <div v-if="!isElectron" class="logs-unavailable">
      Les logs Electron ne sont disponibles qu'en mode desktop.
    </div>

    <template v-else>
      <div class="logs-toolbar">
        <div class="logs-toolbar-left">
          <select v-model="filterLevel" class="logs-select">
            <option v-for="l in LEVELS" :key="l" :value="l">
              {{ l === 'all' ? 'Tous les niveaux' : l.toUpperCase() }}
            </option>
          </select>

          <select v-model="filterService" class="logs-select">
            <option v-for="s in services" :key="s" :value="s">
              {{ s === 'all' ? 'Tous les services' : s }}
            </option>
          </select>

          <input
            v-model="searchQuery"
            class="logs-search"
            placeholder="Rechercher..."
            type="text"
          />
        </div>

        <div class="logs-toolbar-right">
          <span class="logs-count">{{ filtered.length }} / {{ logs.length }}</span>

          <button class="logs-clear-btn" @click="clearLogs">Vider</button>
        </div>
      </div>

      <div class="logs-container">
        <div
          v-for="entry in filtered"
          :key="entry.id"
          class="log-entry"
          :class="levelClass(entry.level)"
        >
          <span class="log-time">{{ formatTime(entry.timestamp) }}</span>
          <span class="log-level-badge">{{ entry.level.toUpperCase() }}</span>
          <span class="log-service">{{ entry.service }}</span>
          <span class="log-message">{{ entry.message }}</span>
          <span v-if="entry.data !== null" class="log-data">
            {{ entry.data && typeof entry.data === 'object' ? JSON.stringify(entry.data) : String(entry.data) }}
          </span>
        </div>

        <div v-if="filtered.length === 0" class="logs-empty">
          Aucun log à afficher.
        </div>
      </div>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.logs-view {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  font-family: monospace;
  font-size: 0.78rem;
}

.logs-unavailable {
  padding: 2rem;
  color: var(--pico-muted-color);
  text-align: center;
}

.logs-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-shrink: 0;
}

.logs-toolbar-left,
.logs-toolbar-right {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.logs-select {
  font-size: 0.75rem;
  padding: 0.2rem 0.4rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 4px;
  background: var(--pico-background-color);
  color: var(--pico-color);
}

.logs-search {
  font-size: 0.75rem;
  padding: 0.2rem 0.4rem;
  margin: 0;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 4px;
  background: var(--pico-background-color);
  color: var(--pico-color);
  width: 160px;
  height: auto;
  line-height: normal;
}

.logs-count {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.logs-clear-btn {
  font-size: 0.72rem;
  padding: 0.2rem 0.5rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 4px;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover {
    background: var(--pico-muted-border-color);
  }
}

.logs-container {
  flex: 1;
  overflow-y: auto;
  padding: 0.25rem 0;
}

.log-entry {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  padding: 0.15rem 0.75rem;
  border-bottom: 1px solid transparent;
  line-height: 1.4;

  &:hover {
    background: var(--pico-muted-border-color);
  }
}

.log-time {
  color: var(--pico-muted-color);
  flex-shrink: 0;
  min-width: 90px;
}

.log-level-badge {
  flex-shrink: 0;
  min-width: 44px;
  font-weight: 600;
  font-size: 0.7rem;
}

.log-service {
  color: var(--pico-primary);
  flex-shrink: 0;
  min-width: 140px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.log-message {
  color: var(--pico-color);
  flex: 1;
  white-space: pre-wrap;
  word-break: break-all;
}

.log-data {
  color: var(--pico-muted-color);
  font-size: 0.7rem;
  max-width: 280px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Level colors */
.log-entry.log-level--debug {
  .log-level-badge { color: var(--pico-muted-color); }
}

.log-entry.log-level--info {
  .log-level-badge { color: #4ade80; }
}

.log-entry.log-level--warn {
  background: rgba(251, 191, 36, 0.05);
  .log-level-badge { color: #fbbf24; }
}

.log-entry.log-level--error {
  background: rgba(248, 113, 113, 0.08);
  .log-level-badge { color: #f87171; }
  .log-message { color: #f87171; }
}

.logs-empty {
  padding: 2rem;
  text-align: center;
  color: var(--pico-muted-color);
}
</style>
