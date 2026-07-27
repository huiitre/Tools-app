<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { usePalworldTierListStore } from '../palworldTierList.store'
import { usePaldexStore } from '../../paldex/paldex.store'
import type { PalworldPalListItem, PalworldElementSummary, PalworldWorkSuitabilitySummary } from '../../paldex/types/paldex.types'
import PalContextTrigger from '../../paldex/components/PalContextTrigger.vue'

interface TierPal extends PalworldPalListItem {
  tier: string
}

const store = usePalworldTierListStore()
const paldexStore = usePaldexStore()

const paldexById = computed(() => {
  const map = new Map<number, PalworldPalListItem>()
  for (const pal of paldexStore.pals) {
    map.set(pal.id, pal)
  }
  return map
})

const CATEGORIES: { id: string; label: string }[] = [
  { id: 'general', label: 'Meilleurs' },
  { id: 'base-work', label: 'Meilleur Travailleur' },
  { id: 'flying-mounts', label: 'Montures Volantes' },
  { id: 'ground-mounts', label: 'Montures Terrestres' },
  { id: 'combat', label: 'Combat' },
]

const SPEED_CATEGORIES = new Set(['flying-mounts', 'ground-mounts'])

const activeCategory = ref('general')
const searchQuery = ref('')

const availableSources = computed(() => Object.keys(store.data ?? {}))
const selectedSource = ref<string | null>(null)

watch(availableSources, sources => {
  if ((!selectedSource.value || !sources.includes(selectedSource.value)) && sources.length) {
    selectedSource.value = sources[0]
  }
}, { immediate: true })

const currentSourceData = computed(() => (selectedSource.value ? store.data?.[selectedSource.value] : null) ?? {})

const currentTiers = computed(() => {
  const groups = currentSourceData.value[activeCategory.value] ?? []
  return groups.map(group => ({
    tier: group.tier,
    pals: group.palIds
      .map(id => paldexById.value.get(id))
      .filter((pal): pal is PalworldPalListItem => !!pal),
  }))
})

const showWorkSuitabilities = computed(() => activeCategory.value === 'base-work')
const showSpeed = computed(() => SPEED_CATEGORIES.has(activeCategory.value))

const normalizedQuery = computed(() => searchQuery.value.trim().toLowerCase())

const allCurrentPals = computed(() => currentTiers.value.flatMap(group => group.pals))

const availableElements = computed<PalworldElementSummary[]>(() => {
  const byId = new Map<number, PalworldElementSummary>()
  for (const pal of allCurrentPals.value) {
    for (const element of pal.elements) {
      if (!byId.has(element.id)) byId.set(element.id, element)
    }
  }
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})

const availableWorkSuitabilities = computed<PalworldWorkSuitabilitySummary[]>(() => {
  if (!showWorkSuitabilities.value) return []
  const byId = new Map<number, PalworldWorkSuitabilitySummary>()
  for (const pal of allCurrentPals.value) {
    for (const ws of pal.workSuitabilities) {
      if (!byId.has(ws.id)) byId.set(ws.id, ws)
    }
  }
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})

const selectedElementIds = ref<Set<number>>(new Set())
const selectedWorkSuitabilityIds = ref<Set<number>>(new Set())

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

const hasActiveFilter = computed(() =>
  !!normalizedQuery.value || selectedElementIds.value.size > 0 || selectedWorkSuitabilityIds.value.size > 0,
)

const matches = (pal: PalworldPalListItem) => {
  if (normalizedQuery.value && !pal.name.toLowerCase().includes(normalizedQuery.value)) return false
  if (selectedElementIds.value.size && !pal.elements.some(e => selectedElementIds.value.has(e.id))) return false
  if (showWorkSuitabilities.value && selectedWorkSuitabilityIds.value.size
      && !pal.workSuitabilities.some(ws => selectedWorkSuitabilityIds.value.has(ws.id))) return false
  return true
}

const totalCount = computed(() => currentTiers.value.reduce((sum, group) => sum + group.pals.length, 0))
const visibleCount = computed(() => currentTiers.value.reduce(
  (sum, group) => sum + group.pals.filter(matches).length, 0,
))

const searchResults = computed<TierPal[]>(() => {
  if (!normalizedQuery.value) return []
  return currentTiers.value.flatMap(group =>
    group.pals.filter(matches).map(pal => ({ ...pal, tier: group.tier })),
  )
})

const isRowAllDimmed = (pals: PalworldPalListItem[]) =>
  hasActiveFilter.value && pals.every(pal => !matches(pal))

onMounted(() => {
  store.ensureLoaded()
})
</script>

<template>
  <div class="tierlist">
    <div class="tierlist-header">
      <nav v-if="availableSources.length > 1" class="source-tabs">
        <button
          v-for="source in availableSources"
          :key="source"
          type="button"
          class="source-tab"
          :class="{ active: selectedSource === source }"
          @click="selectedSource = source"
        >
          {{ source }}
        </button>
      </nav>

      <nav class="category-tabs">
        <button
          v-for="cat in CATEGORIES"
          :key="cat.id"
          type="button"
          class="category-tab"
          :class="{ active: activeCategory === cat.id }"
          @click="activeCategory = cat.id"
        >
          {{ cat.label }}
        </button>
      </nav>

      <div class="search-bar">
        <input v-model="searchQuery" type="search" placeholder="Rechercher un pal..." class="search-input">

        <span class="search-count">
          <span class="count-sep" />
          <strong>{{ visibleCount }}</strong> / {{ totalCount }} pals
        </span>

        <button v-if="normalizedQuery" type="button" class="clear-btn" @click="searchQuery = ''">
          Effacer
        </button>
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
      Chargement de la tier list…
    </div>

    <template v-else>
      <div v-if="normalizedQuery" class="search-results">
        <PalContextTrigger
          v-for="pal in searchResults"
          :key="pal.id + pal.tier"
          :pal="pal"
        >
          <div class="search-result-row">
            <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" loading="lazy">
            <span class="result-elements">
              <span
                v-for="element in pal.elements"
                :key="element.id"
                class="element-icon-crop"
                :title="element.name"
                :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
              />
            </span>
            <span class="result-name">{{ pal.name }}</span>
            <span class="result-tier" :class="`tier-${pal.tier}`">{{ pal.tier }}</span>
          </div>
        </PalContextTrigger>

        <p v-if="searchResults.length === 0" class="empty">Aucun pal trouvé.</p>
      </div>

      <div class="tier-list">
        <div
          v-for="group in currentTiers"
          :key="group.tier"
          class="tier-row"
          :class="{ 'all-dimmed': isRowAllDimmed(group.pals) }"
        >
          <div class="tier-badge" :class="`tier-${group.tier}`">{{ group.tier }}</div>
          <div class="tier-pals">
            <PalContextTrigger
              v-for="pal in group.pals"
              :key="pal.id"
              :pal="pal"
            >
              <div
                class="pal-card"
                :class="{ dimmed: !matches(pal), wide: showWorkSuitabilities && !!pal.workSuitabilities.length }"
              >
                <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="72" height="72" loading="lazy">
                <span class="pal-name">{{ pal.name }}</span>
                <span v-if="pal.elements.length" class="pal-elements-row">
                  <span
                    v-for="element in pal.elements"
                    :key="element.id"
                    class="element-icon-crop"
                    :title="element.name"
                    :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
                  />
                </span>
                <span v-if="showSpeed && pal.runSpeed && pal.rideSprintSpeed" class="pal-speed">
                  {{ pal.runSpeed }} - {{ pal.rideSprintSpeed }}
                </span>
                <span v-if="showWorkSuitabilities && pal.workSuitabilities.length" class="pal-workskills">
                  <span v-for="ws in pal.workSuitabilities" :key="ws.id" class="workskill" :title="ws.name">
                    <img v-if="ws.iconUrl" :src="ws.iconUrl" :alt="ws.name" width="16" height="16" loading="lazy">
                    <span class="workskill-level">{{ ws.level }}</span>
                  </span>
                </span>
              </div>
            </PalContextTrigger>
          </div>
        </div>

        <p v-if="currentTiers.length === 0" class="empty">Aucune donnée pour cette catégorie.</p>
      </div>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.tierlist {
  padding: 2rem;
  max-width: 1100px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

@media (max-width: 640px) {
  .tierlist {
    padding: 1rem;
  }
}

/* ── Header ──────────────────────────────────────────────────────── */
.tierlist-header {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.source-tabs {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.source-tab {
  padding: 0.3rem 0.75rem;
  border-radius: 6px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.72rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin: 0;
  width: auto;

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

.category-tabs {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.category-tab {
  padding: 0.4rem 0.9rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin: 0;
  width: auto;

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-primary);
  }

  &.active {
    background: var(--pico-primary);
    border-color: var(--pico-primary);
    color: var(--pico-primary-inverse, #000);
    font-weight: 700;
  }
}

/* ── Search bar (style Paldex) ───────────────────────────────────── */
.search-bar {
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

.search-count {
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

  &:hover {
    color: var(--pico-color);
    border-color: var(--pico-color);
  }
}

/* ── Element tabs (identique au Paldex) ──────────────────────────── */
.element-tabs {
  display: flex;
  align-items: center;
  justify-content: flex-start;
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

/* Bannières élément (104x32, pictogramme dans le tiers gauche) — recadrage identique au Paldex */
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
  padding: 1.5rem;
  text-align: center;
  color: var(--pico-muted-color);
  font-size: 0.875rem;
  margin: 0;
}

/* ── Tier colors ─────────────────────────────────────────────────── */
.tier-S { background: #ff6b00; }
.tier-A { background: #ffb300; }
.tier-B { background: #3dba5e; }
.tier-C { background: #3a9fd6; }
.tier-D { background: #8878c0; }

/* ── Tier list ───────────────────────────────────────────────────── */
.tier-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.tier-row {
  display: flex;
  align-items: stretch;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-background-color);
}

.tier-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  min-height: 88px;
  font-size: 22px;
  font-weight: 900;
  color: #000;
  flex-shrink: 0;
  transition: opacity 0.2s ease;
}

.tier-row.all-dimmed .tier-badge {
  opacity: 0.35;
}

.tier-pals {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 10px;
  flex: 1;
  align-content: flex-start;
}

/* ── Pal card ────────────────────────────────────────────────────── */
.pal-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 6px;
  border-radius: 8px;
  text-decoration: none;
  width: 84px;
  transition: background 0.15s ease, opacity 0.2s ease;
  border: 1px solid transparent;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 8%, transparent);
    border-color: var(--pico-card-border-color);
  }

  &.dimmed {
    opacity: 0.15;
    pointer-events: none;
  }

  img {
    width: 72px;
    height: 72px;
    border-radius: 6px;
    display: block;
  }

  &.wide {
    width: 96px;
  }
}

.pal-elements-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
}

.pal-name {
  font-size: 10.5px;
  color: var(--pico-color);
  text-align: center;
  line-height: 1.3;
  word-break: break-word;
}

.pal-speed {
  font-size: 9.5px;
  font-weight: 700;
  color: var(--pico-primary);
}

.pal-workskills {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 3px;
}

.workskill {
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

.workskill-level {
  font-size: 9px;
  font-weight: 700;
  color: var(--pico-color);
}

/* ── Search results ──────────────────────────────────────────────── */
.search-results {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.search-result-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-card-background-color);
  text-decoration: none;
  color: var(--pico-color);
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 8%, transparent);
    border-color: var(--pico-primary);
  }

  img {
    width: 36px;
    height: 36px;
    border-radius: 6px;
    flex-shrink: 0;
  }
}

.result-elements {
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex-shrink: 0;
}

.result-name {
  flex: 1;
  font-size: 0.85rem;
}

.result-tier {
  width: 26px;
  height: 26px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 900;
  color: #000;
  flex-shrink: 0;
}
</style>
