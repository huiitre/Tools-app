<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePaldexStore } from '../../paldex/paldex.store'
import { paldexLabel } from '../../paldex/utils/paldexLabel'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'

const props = withDefaults(defineProps<{
  // 'single' (défaut) : clic = sélection immédiate (comportement historique, calculateur/recherche).
  // 'multiple' : clic = coche/décoche dans selectedIds, ne referme rien (utilisé par le Path Finder
  // pour construire la liste des Pals possédés).
  mode?: 'single' | 'multiple'
  selectedIds?: Set<number>
}>(), {
  mode: 'single',
  selectedIds: () => new Set(),
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

type SortKey = 'paldex' | 'name'

const searchQuery = ref('')
const sortKey = ref<SortKey>('paldex')
const sortDir = ref<'asc' | 'desc'>('asc')

const normalizedQuery = computed(() => searchQuery.value.trim().toLowerCase())

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function sortValue(pal: PalworldPalListItem): number | string {
  return sortKey.value === 'name'
    ? pal.name.toLowerCase()
    : String(pal.paldexIndex ?? 9999).padStart(4, '0') + (pal.paldexSuffix ?? '')
}

const visiblePals = computed(() => {
  const filtered = store.pals.filter(pal => !normalizedQuery.value || pal.name.toLowerCase().includes(normalizedQuery.value))
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
      <div class="pal-list">
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
