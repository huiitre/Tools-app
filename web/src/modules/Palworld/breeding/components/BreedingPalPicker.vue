<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePaldexStore } from '../../paldex/paldex.store'
import { usePalworldServerDataStore } from '../../server/serverData.store'
import { paldexLabel } from '../../paldex/utils/paldexLabel'
import type { PalworldElementSummary, PalworldPalListItem } from '../../paldex/types/paldex.types'

const props = withDefaults(defineProps<{
  // 'single' (défaut) : clic = sélection immédiate (comportement historique, calculateur/recherche).
  // 'multiple' : clic = coche/décoche dans selectedIds, ne referme rien (utilisé par le Path Finder
  // pour construire la liste des Pals possédés).
  mode?: 'single' | 'multiple'
  selectedIds?: Set<number>
  display?: 'list' | 'cards'
  showElementFilters?: boolean
  showWorkSuitabilities?: boolean
  showCount?: boolean
}>(), {
  mode: 'single',
  selectedIds: () => new Set(),
  display: 'list',
  showElementFilters: false,
  showWorkSuitabilities: false,
  showCount: false,
})

const emit = defineEmits<{
  select: [pal: PalworldPalListItem]
  toggle: [pal: PalworldPalListItem]
}>()

function handleClick(pal: PalworldPalListItem) {
  if (props.mode === 'multiple') emit('toggle', pal)
  else emit('select', pal)
}

const store = usePaldexStore()
const serverDataStore = usePalworldServerDataStore()

type SortKey = 'paldex' | 'name'

const searchQuery = ref('')
const sortKey = ref<SortKey>('paldex')
const sortDir = ref<'asc' | 'desc'>('asc')
const selectedElementIds = ref<Set<number>>(new Set())

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

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function toggleElement(id: number) {
  const next = new Set(selectedElementIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedElementIds.value = next
}

function sortValue(pal: PalworldPalListItem): number | string {
  return sortKey.value === 'name'
    ? pal.name.toLowerCase()
    : String(pal.paldexIndex ?? 9999).padStart(4, '0') + (pal.paldexSuffix ?? '')
}

const visiblePals = computed(() => {
  const filtered = store.pals.filter(pal =>
    (!normalizedQuery.value || pal.name.toLowerCase().includes(normalizedQuery.value))
    && (!selectedElementIds.value.size || pal.elements.some(element => selectedElementIds.value.has(element.id))))
  return [...filtered].sort((a, b) => {
    const va = sortValue(a)
    const vb = sortValue(b)
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return sortDir.value === 'asc' ? cmp : -cmp
  })
})
</script>

<template>
  <div class="breeding-picker">
    <div class="breeding-picker-toolbar">
      <input
        v-model="searchQuery"
        type="search"
        placeholder="Rechercher un Pal..."
        class="search-input"
      >

      <div v-if="showElementFilters && availableElements.length" class="element-filters">
        <button
          v-for="element in availableElements"
          :key="element.id"
          type="button"
          class="element-filter"
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
      </div>

      <div class="sort-controls">
        <select v-model="sortKey" class="toolbar-select">
          <option value="paldex">Paldex</option>
          <option value="name">Nom</option>
        </select>

        <button
          type="button"
          class="toolbar-btn"
          :title="sortDir === 'asc' ? 'Croissant' : 'Décroissant'"
          @click="toggleSortDir"
        >
          <i class="mdi" :class="sortDir === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" />
        </button>
      </div>

      <span v-if="showCount" class="pal-count">
        <strong>{{ visiblePals.length }}</strong> / {{ store.pals.length }} Pals
      </span>
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
      <div v-if="display === 'list'" class="pal-list">
        <button
          v-for="pal in visiblePals"
          :key="pal.id"
          type="button"
          class="pal-row"
          :class="{ selected: selectedIds.has(pal.id) }"
          @click="handleClick(pal)"
        >
          <i
            v-if="mode === 'multiple'"
            class="mdi pal-row-check"
            :class="selectedIds.has(pal.id) ? 'mdi-check-circle' : 'mdi-circle-outline'"
          />
          <span class="pal-row-index">{{ paldexLabel(pal) }}</span>
          <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="28" height="28" loading="lazy">
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
        </button>
      </div>

      <div v-else class="pal-grid">
        <button
          v-for="pal in visiblePals"
          :key="pal.id"
          type="button"
          class="pal-card"
          :class="{ selected: selectedIds.has(pal.id) }"
          @click="handleClick(pal)"
        >
          <i
            v-if="mode === 'multiple'"
            class="mdi pal-card-check"
            :class="selectedIds.has(pal.id) ? 'mdi-check-circle' : 'mdi-circle-outline'"
          />
          <span class="pal-index">{{ paldexLabel(pal) }}</span>
          <span v-if="serverDataStore.selectedPalCounts.get(pal.id)" class="server-pal-count">{{ serverDataStore.selectedPalCounts.get(pal.id) }}</span>
          <img v-if="pal.imageUrl" :src="pal.imageUrl" :alt="pal.name" width="84" height="84" loading="lazy">
          <i v-else class="mdi mdi-help pal-card-no-image" />
          <span class="pal-name">{{ pal.name }}</span>
          <span v-if="pal.elements.length" class="pal-elements">
            <span
              v-for="element in pal.elements"
              :key="element.id"
              class="element-icon-crop"
              :title="element.name"
              :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}"
            />
          </span>
          <span v-if="showWorkSuitabilities && pal.workSuitabilities.length" class="pal-worksuitabilities">
            <span v-for="workSuitability in pal.workSuitabilities" :key="workSuitability.id" class="worksuitability" :title="workSuitability.name">
              <img v-if="workSuitability.iconUrl" :src="workSuitability.iconUrl" :alt="workSuitability.name" width="16" height="16" loading="lazy">
              <span class="worksuitability-level">{{ workSuitability.level }}</span>
            </span>
          </span>
        </button>
      </div>

      <p v-if="visiblePals.length === 0" class="empty">Aucun Pal ne correspond à la recherche.</p>
    </template>
  </div>
</template>

<style lang="scss" scoped>
.breeding-picker {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.breeding-picker-toolbar {
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

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.element-filters {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  flex-wrap: wrap;
}

.element-filter {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  width: auto;
  height: 2rem;
  margin: 0;
  padding: 0.25rem 0.5rem;
  border-radius: var(--pico-border-radius);
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.75rem;
  cursor: pointer;

  &:hover,
  &.active {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  .element-icon-crop {
    width: 18px;
    height: 18px;
  }
}

.toolbar-select {
  margin: 0;
  height: 2rem;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
  width: auto;
  min-width: 120px;
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

.pal-count {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;

  strong {
    color: var(--pico-primary);
  }
}

.status {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 2rem 0;
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

.pal-list {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  max-height: 360px;
  overflow-y: auto;
}

.pal-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  align-content: start;
  grid-auto-rows: max-content;
  gap: 0.85rem;
  max-height: 560px;
  overflow-y: auto;
  padding: 0.1rem;
}

.pal-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  margin: 0;
  padding: 0.85rem 0.6rem;
  border-radius: 10px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  color: inherit;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover,
  &.selected {
    border-color: var(--pico-primary);
  }

  &.selected {
    background: color-mix(in srgb, var(--pico-primary) 8%, var(--pico-card-background-color));
  }

  > img {
    width: 84px;
    height: 84px;
    border-radius: 6px;
    display: block;
    object-fit: cover;
  }
}

.pal-card-check {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
  color: var(--pico-muted-color);
  font-size: 1rem;
}

.pal-card.selected .pal-card-check {
  color: var(--pico-primary);
}

.pal-card-no-image {
  width: 84px;
  height: 84px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: var(--pico-muted-border-color);
  color: var(--pico-muted-color);
  font-size: 1.5rem;
}

.pal-index {
  font-size: 0.74rem;
  color: var(--pico-muted-color);
  font-weight: 600;
}

.server-pal-count {
  position: absolute;
  top: .45rem;
  right: .45rem;
  display: grid;
  place-items: center;
  min-width: 1.25rem;
  height: 1.25rem;
  padding: 0 .25rem;
  border: 1px solid var(--pico-primary);
  border-radius: 999px;
  background: var(--pico-card-background-color);
  color: var(--pico-primary);
  font-size: .65rem;
  font-weight: 700;
}

.pal-name {
  font-size: 0.86rem;
  color: var(--pico-color);
  text-align: center;
  line-height: 1.3;
  word-break: break-word;
}

.pal-elements {
  display: flex;
  gap: 0.3rem;
  margin-top: 0.15rem;

  .element-icon-crop {
    width: 23px;
    height: 23px;
  }
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
  gap: 3px;
  padding: 2px 4px;
  border-radius: 4px;
  background: color-mix(in srgb, var(--pico-color) 8%, transparent);

  img {
    width: 16px;
    height: 16px;
    border-radius: 0;
  }
}

.worksuitability-level {
  font-size: 10px;
  font-weight: 700;
  color: var(--pico-color);
}

.pal-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.35rem 0.6rem;
  border-radius: 6px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  cursor: pointer;
  margin: 0;
  width: 100%;
  text-align: left;
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    border-color: var(--pico-primary);
  }

  &.selected {
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
  }

  img {
    width: 28px;
    height: 28px;
    border-radius: 4px;
    display: block;
    flex-shrink: 0;
    object-fit: cover;
  }
}

.pal-row-check {
  flex-shrink: 0;
  color: var(--pico-muted-color);
  font-size: 1rem;
}

.pal-row.selected .pal-row-check {
  color: var(--pico-primary);
}

.pal-row-index {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  font-weight: 600;
  flex-shrink: 0;
  width: 3.2rem;
}

.pal-row-name {
  font-size: 0.82rem;
  color: var(--pico-color);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pal-row-elements {
  display: flex;
  gap: 0.25rem;
  flex-shrink: 0;
}

.element-icon-crop {
  display: inline-block;
  flex-shrink: 0;
  width: 18px;
  height: 18px;
  border-radius: 4px;
  background-repeat: no-repeat;
  background-position: left center;
  background-size: auto 100%;
}
</style>
