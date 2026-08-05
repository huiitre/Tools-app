<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { usePaldexStore } from '../../paldex/paldex.store'
import { useBreedingStore } from '../breeding.store'
import { fetchBreedingParents, fetchBreedingAsParent } from '../fetch/breeding.fetch'
import BreedingPalPicker from '../components/BreedingPalPicker.vue'
import BreedingSquare from '../components/BreedingSquare.vue'
import BreedingCombinationRow from '../components/BreedingCombinationRow.vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingCombination, BreedingSearchMode } from '../types/breeding.types'

const route = useRoute()
const router = useRouter()
const paldexStore = usePaldexStore()
const breedingStore = useBreedingStore()

const mode = ref<BreedingSearchMode>('child')
const combinations = ref<BreedingCombination[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const resultSearchQuery = ref('')

const palById = computed(() => new Map(paldexStore.pals.map(pal => [pal.id, pal])))

function resolvePal(id: number): PalworldPalListItem | null {
  return palById.value.get(id) ?? null
}

const filteredCombinations = computed(() => {
  const query = resultSearchQuery.value.trim().toLowerCase()
  if (!query) return combinations.value

  return combinations.value.filter(combo =>
    combo.parentA.name.toLowerCase().includes(query)
    || combo.parentB.name.toLowerCase().includes(query)
    || combo.child.name.toLowerCase().includes(query))
})

async function loadCombinations() {
  resultSearchQuery.value = ''

  if (!breedingStore.selectedPal) {
    combinations.value = []
    return
  }

  loading.value = true
  error.value = null
  try {
    combinations.value = mode.value === 'child'
      ? await fetchBreedingParents(breedingStore.selectedPal.id)
      : await fetchBreedingAsParent(breedingStore.selectedPal.id)
  } catch {
    combinations.value = []
    error.value = 'Impossible de charger les combinaisons.'
  } finally {
    loading.value = false
  }
}

function syncUrl() {
  router.replace({
    name: 'palworld-breeding-search',
    query: { ...(breedingStore.selectedPalId ? { pal: breedingStore.selectedPalId } : {}), mode: mode.value },
  })
}

function selectMode(next: BreedingSearchMode) {
  if (mode.value === next) return
  mode.value = next
  syncUrl()
  loadCombinations()
}

function handleSelectPal(pal: PalworldPalListItem) {
  breedingStore.selectPal(pal)
  syncUrl()
  loadCombinations()
}

function preselectFromUrl() {
  const rawMode = route.query.mode
  if (rawMode === 'parent' || rawMode === 'child') mode.value = rawMode

  const rawId = route.query.pal
  if (!rawId) return
  const palId = Number(Array.isArray(rawId) ? rawId[0] : rawId)
  if (!Number.isFinite(palId)) return

  const pal = paldexStore.pals.find(p => p.id === palId)
  if (pal) breedingStore.selectPal(pal)
}

onMounted(async () => {
  await paldexStore.ensureLoaded()
  preselectFromUrl()
  if (breedingStore.selectedPal) loadCombinations()
})
</script>

<template>
  <div class="search-view">
    <aside class="search-selection">
      <div class="search-selection-content">
        <div class="selected-pal">
          <BreedingSquare label="Pal sélectionné" :pal="breedingStore.selectedPal" />
        </div>
        <BreedingPalPicker display="cards" show-element-filters show-count @select="handleSelectPal" />
      </div>
    </aside>

    <section class="search-results">
      <header class="results-header">
        <div class="mode-tabs">
        <button
          type="button"
          class="mode-tab"
          :class="{ active: mode === 'child' }"
          @click="selectMode('child')"
        >
          En tant qu'enfant
        </button>
        <button
          type="button"
          class="mode-tab"
          :class="{ active: mode === 'parent' }"
          @click="selectMode('parent')"
        >
          En tant que parent
        </button>
      </div>
        <span v-if="breedingStore.selectedPal" class="selected-label">
          Combinaisons de {{ breedingStore.selectedPal.name }}
        </span>
      </header>

      <div v-if="!breedingStore.selectedPal" class="empty-state">
        Sélectionne un Pal dans la colonne de gauche pour afficher ses combinaisons d'élevage.
      </div>

      <template v-else>
        <p v-if="error" class="error-banner">
          <i class="mdi mdi-alert-circle-outline" />
          {{ error }}
        </p>

        <div v-else-if="loading" class="status">
          <span class="spinner" />
          Chargement…
        </div>

        <template v-else>
          <div v-if="combinations.length" class="results-toolbar">
            <input
              v-model="resultSearchQuery"
              type="search"
              placeholder="Rechercher dans les combinaisons..."
              class="search-input"
            >
            <span class="results-count">
              <strong>{{ filteredCombinations.length }}</strong> / {{ combinations.length }}
            </span>
          </div>

          <div class="combination-list">
            <BreedingCombinationRow
              v-for="(combo, index) in filteredCombinations"
              :key="index"
              :parent-a="resolvePal(combo.parentA.id)"
              :parent-b="resolvePal(combo.parentB.id)"
              :child="resolvePal(combo.child.id)"
              :parent-a-gender="combo.parentAGender"
              :parent-b-gender="combo.parentBGender"
              :rule="combo.rule"
            />
          </div>

          <p v-if="combinations.length === 0" class="empty">Aucune combinaison trouvée.</p>
          <p v-else-if="filteredCombinations.length === 0" class="empty">Aucune combinaison ne correspond à la recherche.</p>
        </template>
      </template>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.search-view {
  display: grid;
  grid-template-columns: minmax(420px, 1.15fr) minmax(360px, 0.85fr);
  gap: 2rem;
  align-items: stretch;
  max-width: 1560px;
  margin: 0 auto;
  padding-bottom: 2rem;
}

@media (max-width: 1050px) {
  .search-view {
    grid-template-columns: 1fr;
  }
}

.search-selection {
  min-width: 0;
}

.search-selection-content {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  height: calc(100dvh - 9rem);
  overflow: hidden;
  position: sticky;
  top: calc(56px + 1rem);
}

.search-selection-content :deep(.breeding-picker) {
  flex: 1;
  min-height: 0;
}

.search-selection-content :deep(.pal-grid) {
  flex: 1;
  min-height: 0;
  max-height: none;
}

.selected-pal {
  display: flex;
  justify-content: center;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.search-results {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

@media (max-width: 1050px) {
  .search-view {
    align-items: flex-start;
  }

  .search-selection-content {
    position: static;
    height: auto;
    overflow: visible;
  }

  .search-selection-content :deep(.pal-grid) {
    max-height: 560px;
  }
}

.results-header {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.mode-tabs {
  display: flex;
  gap: 0.5rem;
}

.mode-tab {
  flex: 1;
  margin: 0;
  padding: 0.5rem 0.75rem;
  font-size: 0.78rem;
  font-weight: 600;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-primary);
  }

  &.active {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
  }
}

.empty-state {
  color: var(--pico-muted-color);
  text-align: center;
  padding: 3rem 1rem;
}

.selected-label {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.results-toolbar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.search-input {
  margin: 0;
  height: 2rem;
  font-size: 0.75rem;
  width: 260px;
}

.results-count {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;

  strong { color: var(--pico-primary); font-weight: 700; }
}

.combination-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
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
  margin: 0;
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 2rem 0;
}
</style>
