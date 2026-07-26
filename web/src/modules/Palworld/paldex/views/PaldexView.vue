<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { usePaldexStore } from '../paldex.store'
import type {
  PalworldElementSummary,
  PalworldWorkSuitabilitySummary,
  PalworldPalListItem,
  PaldexSortKey,
} from '../types/paldex.types'

const store = usePaldexStore()

const SORT_OPTIONS: { id: PaldexSortKey; label: string }[] = [
  { id: 'paldex', label: 'Paldex' },
  { id: 'name', label: 'Nom' },
  { id: 'hp', label: 'PV' },
  { id: 'attack', label: 'Attaque' },
  { id: 'defense', label: 'Défense' },
  { id: 'workSpeed', label: "Niveau d'aptitude" },
]

const searchQuery = ref('')
const selectedElementIds = ref<Set<number>>(new Set())
const selectedWorkSuitabilityIds = ref<Set<number>>(new Set())
const sortKey = ref<PaldexSortKey>('paldex')
const sortDir = ref<'asc' | 'desc'>('asc')

const normalizedQuery = computed(() => searchQuery.value.trim().toLowerCase())

const availableElements = computed<PalworldElementSummary[]>(() => {
  const byId = new Map<number, PalworldElementSummary>()
  for (const pal of store.pals) {
    for (const element of pal.elements) {
      if (!byId.has(element.id)) byId.set(element.id, element)
    }
  }
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})

const availableWorkSuitabilities = computed<PalworldWorkSuitabilitySummary[]>(() => {
  const byId = new Map<number, PalworldWorkSuitabilitySummary>()
  for (const pal of store.pals) {
    for (const ws of pal.workSuitabilities) {
      if (!byId.has(ws.id)) byId.set(ws.id, ws)
    }
  }
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})

function toggleElement(id: number) {
  const next = new Set(selectedElementIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedElementIds.value = next
}

function toggleWorkSuitability(id: number) {
  const next = new Set(selectedWorkSuitabilityIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedWorkSuitabilityIds.value = next
}

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function matchesSearch(pal: PalworldPalListItem) {
  return !normalizedQuery.value || pal.name.toLowerCase().includes(normalizedQuery.value)
}

function matchesElements(pal: PalworldPalListItem) {
  if (!selectedElementIds.value.size) return true
  return pal.elements.some(e => selectedElementIds.value.has(e.id))
}

function matchesWorkSuitabilities(pal: PalworldPalListItem) {
  if (!selectedWorkSuitabilityIds.value.size) return true
  return pal.workSuitabilities.some(ws => selectedWorkSuitabilityIds.value.has(ws.id))
}

function sortValue(pal: PalworldPalListItem): number | string {
  switch (sortKey.value) {
    case 'name': return pal.name.toLowerCase()
    case 'hp': return pal.baseHp ?? -1
    case 'attack': return pal.baseAttack ?? -1
    case 'defense': return pal.baseDefense ?? -1
    case 'workSpeed': {
      const levels = pal.workSuitabilities
        .filter(ws => selectedWorkSuitabilityIds.value.has(ws.id))
        .map(ws => ws.level)
      return levels.length ? Math.max(...levels) : -1
    }
    default: return String(pal.paldexIndex ?? 9999).padStart(4, '0') + (pal.paldexSuffix ?? '')
  }
}

const visiblePals = computed(() => {
  const filtered = store.pals.filter(pal => matchesSearch(pal) && matchesElements(pal) && matchesWorkSuitabilities(pal))
  const sorted = [...filtered].sort((a, b) => {
    const va = sortValue(a)
    const vb = sortValue(b)
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return sortDir.value === 'asc' ? cmp : -cmp
  })
  return sorted
})

function paldexLabel(pal: PalworldPalListItem): string {
  if (pal.paldexIndex === null) return '—'
  return `#${String(pal.paldexIndex).padStart(3, '0')}${pal.paldexSuffix ?? ''}`
}

onMounted(() => {
  store.ensureLoaded()
})
</script>

<template>
  <div class="paldex">
    <div class="paldex-header">
      <div class="paldex-toolbar">
        <input
          v-model="searchQuery"
          type="search"
          placeholder="Rechercher un Pal..."
          class="search-input"
        >

        <div class="sort-controls">
          <select v-model="sortKey" class="toolbar-select sort-select">
            <option v-for="opt in SORT_OPTIONS" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
          </select>

          <button
            type="button"
            class="toolbar-btn sort-order-btn"
            :title="sortDir === 'asc' ? 'Croissant' : 'Décroissant'"
            @click="toggleSortDir"
          >
            <i class="mdi" :class="sortDir === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" />
          </button>
        </div>

        <span class="paldex-count">
          <span class="count-sep" />
          <strong>{{ visiblePals.length }}</strong> / {{ store.pals.length }} Pals
        </span>
      </div>

      <nav v-if="availableElements.length || availableWorkSuitabilities.length" class="element-tabs">
        <button
          v-for="element in availableElements"
          :key="`element-${element.id}`"
          type="button"
          class="element-tab"
          :class="{ active: selectedElementIds.has(element.id) }"
          @click="toggleElement(element.id)"
        >
          <span
            v-if="element.iconUrl"
            class="element-icon-crop"
            :style="{ backgroundImage: `url(${element.iconUrl})` }"
          />
          {{ element.name }}
        </button>

        <span v-if="availableElements.length && availableWorkSuitabilities.length" class="filter-separator" />

        <button
          v-for="ws in availableWorkSuitabilities"
          :key="`ws-${ws.id}`"
          type="button"
          class="element-tab"
          :class="{ active: selectedWorkSuitabilityIds.has(ws.id) }"
          @click="toggleWorkSuitability(ws.id)"
        >
          <img v-if="ws.iconUrl" :src="ws.iconUrl" :alt="ws.name" width="16" height="16" loading="lazy">
          {{ ws.name }}
        </button>

        <button
          v-if="selectedElementIds.size || selectedWorkSuitabilityIds.size"
          type="button"
          class="clear-btn"
          @click="selectedElementIds = new Set(); selectedWorkSuitabilityIds = new Set()"
        >
          Effacer les filtres
        </button>
      </nav>
    </div>

    <div v-if="store.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ store.error }}
    </div>

    <div v-else-if="store.loading" class="status">
      <span class="spinner" />
      Chargement du Paldex…
    </div>

    <template v-else>
      <div class="pal-grid">
        <div v-for="pal in visiblePals" :key="pal.id" class="pal-card">
          <span class="pal-index">{{ paldexLabel(pal) }}</span>
          <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="72" height="72" loading="lazy">
          <span class="pal-name">{{ pal.name }}</span>
          <span class="pal-elements">
            <span
              v-for="element in pal.elements"
              :key="element.id"
              class="element-icon-crop"
              :title="element.name"
              :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
            />
          </span>
          <span v-if="pal.workSuitabilities.length" class="pal-worksuitabilities">
            <span v-for="ws in pal.workSuitabilities" :key="ws.id" class="worksuitability" :title="ws.name">
              <img v-if="ws.iconUrl" :src="ws.iconUrl" :alt="ws.name" width="14" height="14" loading="lazy">
              <span class="worksuitability-level">{{ ws.level }}</span>
            </span>
          </span>
        </div>
      </div>

      <p v-if="visiblePals.length === 0" class="empty">Aucun Pal ne correspond à la recherche.</p>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.paldex {
  padding: 2rem;
  max-width: 1300px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

@media (max-width: 640px) {
  .paldex {
    padding: 1rem;
  }
}

/* ── Header ──────────────────────────────────────────────────────── */
.paldex-header {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.paldex-toolbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.search-input {
  margin: 0;
  height: 2rem;
  font-size: 0.75rem;
  width: 220px;
}

.toolbar-select {
  margin: 0;
  height: 2rem;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
  width: auto;

  &.sort-select { min-width: 150px; }
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.toolbar-btn {
  margin: 0;
  height: 2rem;
  width: 2rem;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--pico-form-element-background-color);
  border: 1px solid var(--pico-form-element-border-color);
  color: var(--pico-form-element-color);
  cursor: pointer;
  border-radius: var(--pico-border-radius);
  flex-shrink: 0;
  transition: all 0.2s;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  i { font-size: 1rem; }
}

.paldex-count {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
  margin-left: auto;

  strong { color: var(--pico-primary); font-weight: 700; }
}

.count-sep {
  display: inline-block;
  width: 1px;
  height: 1rem;
  background: var(--pico-muted-border-color);
  margin-right: 0.15rem;
}

.clear-btn {
  padding: 0.4rem 0.8rem;
  font-size: 0.78rem;
  width: auto;
  margin: 0;
  border-radius: 6px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-color);
  }
}

/* ── Element tabs ────────────────────────────────────────────────── */
.element-tabs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.filter-separator {
  width: 1px;
  align-self: stretch;
  min-height: 1.5rem;
  background: var(--pico-card-border-color);
  margin: 0 0.25rem;
}

.element-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.8rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin: 0;
  width: auto;

  img { border-radius: 3px; }

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-primary);
  }

  &.active {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    font-weight: 700;
  }
}

/* ── Status / error ──────────────────────────────────────────────── */
.status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 3rem 0;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--pico-card-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 2rem 0;
}

/* ── Pal grid ────────────────────────────────────────────────────── */
.pal-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 0.75rem;
}

.pal-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 0.75rem 0.5rem;
  border-radius: 10px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    border-color: var(--pico-primary);
  }

  img {
    width: 72px;
    height: 72px;
    border-radius: 6px;
    display: block;
  }
}

.pal-index {
  font-size: 0.68rem;
  color: var(--pico-muted-color);
  font-weight: 600;
}

.pal-name {
  font-size: 0.78rem;
  color: var(--pico-color);
  text-align: center;
  line-height: 1.3;
  word-break: break-word;
}

.pal-elements {
  display: flex;
  gap: 0.25rem;
  margin-top: 0.15rem;
}

/* Les icônes d'élément sont des bannières rectangulaires 104×32 (pictogramme
   dans le tiers gauche, bandeau de couleur uni sur le reste) — on recadre sur
   la partie utile plutôt que d'écraser tout le rectangle dans un carré. */
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

.pal-worksuitabilities {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 3px;
  margin-top: 0.15rem;
}

.worksuitability {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 1px 3px;
  border-radius: 4px;
  background: color-mix(in srgb, var(--pico-color) 8%, transparent);

  img {
    width: 14px;
    height: 14px;
    border-radius: 0;
  }
}

.worksuitability-level {
  font-size: 9px;
  font-weight: 700;
  color: var(--pico-color);
}
</style>
