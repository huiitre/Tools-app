<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useTemtemdexStore } from '../temtemdex.store'
import { dexNumber, typesOf } from '../../shared/temtem.helpers'
import TemtemContextTrigger from '@/modules/Temtem/shared/components/TemtemContextTrigger.vue'
import AddToTeamPopup from '../components/AddToTeamPopup.vue'
import type { TemtemSummary, TemtemStats } from '@/modules/Temtem/shared/types/temtem.types'

type SortKey = 'dex' | 'name' | 'total' | keyof TemtemStats

const store = useTemtemdexStore()

const STORAGE_KEY_SORT = 'temtem.temtemdex.sort'

const SORT_OPTIONS: { id: SortKey; label: string }[] = [
  { id: 'dex', label: 'Temtemdex' },
  { id: 'name', label: 'Nom' },
  { id: 'total', label: 'Total des stats' },
  { id: 'hp', label: 'PV' },
  { id: 'stamina', label: 'Endurance' },
  { id: 'speed', label: 'Vitesse' },
  { id: 'attack', label: 'Attaque' },
  { id: 'defense', label: 'Défense' },
  { id: 'specialAttack', label: 'Att. spéciale' },
  { id: 'specialDefense', label: 'Déf. spéciale' },
]

const searchQuery = ref('')
const selectedTypeIds = ref<Set<number>>(new Set())
const sortKey = ref<SortKey>('dex')
const sortDir = ref<'asc' | 'desc'>('asc')
const temtemToAdd = ref<TemtemSummary | null>(null)

const normalizedQuery = computed(() => searchQuery.value.trim().toLowerCase())

function toggleType(id: number) {
  const next = new Set(selectedTypeIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedTypeIds.value = next
}

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function matchesSearch(temtem: TemtemSummary) {
  return !normalizedQuery.value || temtem.name.toLowerCase().includes(normalizedQuery.value)
}

// Plusieurs types cochés = « l'un OU l'autre », comme les éléments du Paldex : cocher Feu et Eau
// montre les deux familles, pas leur seule intersection.
function matchesTypes(temtem: TemtemSummary) {
  if (!selectedTypeIds.value.size) return true
  return typesOf(temtem).some(type => selectedTypeIds.value.has(type.id))
}

function statTotal(temtem: TemtemSummary) {
  return Object.values(temtem.stats).reduce((sum, value) => sum + value, 0)
}

function sortValue(temtem: TemtemSummary): number | string {
  switch (sortKey.value) {
    case 'dex': return temtem.id
    case 'name': return temtem.name.toLowerCase()
    case 'total': return statTotal(temtem)
    default: return temtem.stats[sortKey.value]
  }
}

const visibleTemtem = computed(() => {
  const filtered = store.temtem.filter(temtem => matchesSearch(temtem) && matchesTypes(temtem))
  return [...filtered].sort((a, b) => {
    const va = sortValue(a)
    const vb = sortValue(b)
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return sortDir.value === 'asc' ? cmp : -cmp
  })
})

function hydrateSort() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_SORT)
    if (!raw) return
    const data = JSON.parse(raw)
    if (data.sortKey) sortKey.value = data.sortKey
    if (data.sortDir) sortDir.value = data.sortDir
  } catch (e) {
    console.warn('Failed to hydrate Temtemdex sort', e)
  }
}

watch([sortKey, sortDir], () => {
  localStorage.setItem(STORAGE_KEY_SORT, JSON.stringify({ sortKey: sortKey.value, sortDir: sortDir.value }))
})

onMounted(() => {
  hydrateSort()
})

// Chargement du catalogue géré au niveau parent (Temtem.vue), partagé avec « Mes équipes ».
</script>

<template>
  <div class="temtemdex">
    <div class="temtemdex-header">
      <div class="temtemdex-toolbar">
        <input
          v-model="searchQuery"
          type="search"
          placeholder="Rechercher un Temtem..."
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

        <span class="temtemdex-count">
          <span class="count-sep" />
          <strong>{{ visibleTemtem.length }}</strong> / {{ store.temtem.length }} Temtem
        </span>
      </div>

      <nav v-if="store.types.length" class="type-tabs">
        <button
          v-for="type in store.types"
          :key="type.id"
          type="button"
          class="type-tab"
          :class="{ active: selectedTypeIds.has(type.id) }"
          @click="toggleType(type.id)"
        >
          <img v-if="type.imageUrl" :src="type.imageUrl" :alt="type.name" width="16" height="16" loading="lazy">
          {{ type.name }}
        </button>

        <button
          v-if="selectedTypeIds.size"
          type="button"
          class="clear-btn"
          @click="selectedTypeIds = new Set()"
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
      Chargement du Temtemdex…
    </div>

    <template v-else>
      <div class="temtem-grid">
        <TemtemContextTrigger
          v-for="temtem in visibleTemtem"
          :key="temtem.id"
          :temtem="temtem"
          :disabled="temtemToAdd !== null"
        >
          <div class="temtem-card">
            <button
              type="button"
              class="add-btn"
              title="Ajouter à une équipe"
              @click.stop="temtemToAdd = temtem"
            >
              <i class="mdi mdi-plus" />
            </button>

            <span class="temtem-index">{{ dexNumber(temtem) }}</span>
            <img v-if="temtem.imageUrl" :src="temtem.imageUrl" :alt="temtem.name" width="72" height="72" loading="lazy">
            <span class="temtem-name">{{ temtem.name }}</span>
            <span class="temtem-types">
              <img
                v-for="type in typesOf(temtem)"
                :key="type.id"
                :src="type.imageUrl ?? ''"
                :alt="type.name"
                :title="type.name"
                class="type-icon"
                loading="lazy"
              >
            </span>
          </div>
        </TemtemContextTrigger>
      </div>

      <p v-if="visibleTemtem.length === 0" class="empty">Aucun Temtem ne correspond à la recherche.</p>
    </template>

    <AddToTeamPopup
      v-if="temtemToAdd"
      :temtem="temtemToAdd"
      @close="temtemToAdd = null"
    />
  </div>
</template>

<style lang="scss" scoped>
.temtemdex {
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
  .temtemdex {
    padding: 1rem;
  }
}

/* ── Header ──────────────────────────────────────────────────────── */
.temtemdex-header {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.temtemdex-toolbar {
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

.temtemdex-count {
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
    color: var(--pico-contrast);
    border-color: var(--pico-contrast);
  }
}

/* ── Type tabs ───────────────────────────────────────────────────── */
.type-tabs {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.type-tab {
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

  &:hover {
    color: var(--pico-contrast);
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

/* ── Temtem grid ─────────────────────────────────────────────────── */
.temtem-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 0.85rem;
}

.temtem-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  height: 100%;
  box-sizing: border-box;
  gap: 5px;
  padding: 0.85rem 0.6rem;
  border-radius: 10px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    border-color: var(--pico-primary);

    .add-btn { opacity: 1; }
  }

  img {
    width: 84px;
    height: 84px;
    border-radius: 6px;
    display: block;
  }
}

/* Discret jusqu'au survol de la carte, comme sur les cartes de skins Valorant. */
.add-btn {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;

  display: grid;
  place-items: center;
  width: 1.5rem;
  height: 1.5rem;
  margin: 0;
  padding: 0;

  border: 1px solid var(--pico-card-border-color);
  border-radius: 999px;
  background: var(--pico-card-background-color);
  color: var(--pico-muted-color);
  cursor: pointer;

  opacity: 0;
  transition: opacity 0.2s ease, color 0.15s ease, border-color 0.15s ease;

  &:hover {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
  }

  i { font-size: 0.95rem; }
}

.temtem-index {
  font-size: 0.74rem;
  color: var(--pico-muted-color);
  font-weight: 600;
}

.temtem-name {
  font-size: 0.86rem;
  color: var(--pico-color);
  text-align: center;
  line-height: 1.3;
  word-break: break-word;
}

.temtem-types {
  display: flex;
  gap: 0.3rem;
  margin-top: 0.15rem;
}

.temtem-card .type-icon {
  width: 22px;
  height: 22px;
  border-radius: 4px;
}
</style>
