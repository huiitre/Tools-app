<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import { normalizeSearchText } from '@/utils/searchNormalize'
import { useItemsStore } from '../items.store'
import { categoryLabel } from '../utils/categoryLabel'
import ItemSellSimulationModal from '../components/ItemSellSimulationModal.vue'
import type { ItemCatalogEntry } from '../types/items.types'

type SortKey = 'category' | 'name' | 'price'

const SORT_OPTIONS: { id: SortKey; label: string }[] = [
  { id: 'category', label: 'Catégorie' },
  { id: 'name', label: 'Nom' },
  { id: 'price', label: 'Prix' },
]

const store = useItemsStore()
const searchQuery = ref('')
const selectedCategory = ref<string | null>(null)
const sortKey = ref<SortKey>('category')
const sortDir = ref<'asc' | 'desc'>('asc')

const normalizedQuery = computed(() => normalizeSearchText(searchQuery.value.trim()))

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function sortValue(item: ItemCatalogEntry): number | string {
  switch (sortKey.value) {
    case 'name': return item.name.toLowerCase()
    case 'price': return item.price ?? -1
    default: return (item.category ?? 'zzz').toLowerCase() + '|' + item.name.toLowerCase()
  }
}

const availableCategories = computed(() => {
  const byCategory = new Map<string, number>()
  for (const item of store.items) {
    const key = item.category ?? 'Autre'
    byCategory.set(key, (byCategory.get(key) ?? 0) + 1)
  }
  return [...byCategory.entries()].sort((a, b) => b[1] - a[1])
})

function matchesSearch(item: ItemCatalogEntry): boolean {
  return !normalizedQuery.value || normalizeSearchText(item.name).includes(normalizedQuery.value)
}

function matchesCategory(item: ItemCatalogEntry): boolean {
  if (!selectedCategory.value) return true
  return (item.category ?? 'Autre') === selectedCategory.value
}

const visibleItems = computed(() => {
  const filtered = store.items.filter(item => matchesSearch(item) && matchesCategory(item))
  const sorted = [...filtered].sort((a, b) => {
    const va = sortValue(a)
    const vb = sortValue(b)
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return sortDir.value === 'asc' ? cmp : -cmp
  })
  return sorted
})

function toggleCategory(category: string) {
  selectedCategory.value = selectedCategory.value === category ? null : category
}

// Règle donnée par l'utilisateur (pas dans les données extraites, aucun champ "SellPrice" n'existe) :
// le prix de vente à un marchand vaut 10% du prix d'achat, pour les items en or uniquement.
function sellPrice(item: ItemCatalogEntry): number {
  return Math.floor((item.price ?? 0) * 0.1)
}

const selectedItem = ref<ItemCatalogEntry | null>(null)

onMounted(() => {
  store.ensureLoaded()
})
</script>

<template>
  <div class="items-page">
    <div class="items-header">
      <div class="items-toolbar">
        <input
          v-model="searchQuery"
          type="search"
          placeholder="Rechercher un objet..."
          class="search-input"
        >

        <div class="sort-controls">
          <select v-model="sortKey" class="sort-select">
            <option v-for="opt in SORT_OPTIONS" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
          </select>

          <button
            type="button"
            class="sort-order-btn"
            :title="sortDir === 'asc' ? 'Croissant' : 'Décroissant'"
            @click="toggleSortDir"
          >
            <i class="mdi" :class="sortDir === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" />
          </button>
        </div>

        <span class="items-count">
          <strong>{{ visibleItems.length }}</strong> / {{ store.items.length }} objets
        </span>
      </div>

      <nav v-if="availableCategories.length" class="category-tabs">
        <button
          v-for="[category, count] in availableCategories"
          :key="category"
          type="button"
          class="category-tab"
          :class="{ active: selectedCategory === category }"
          @click="toggleCategory(category)"
        >
          {{ categoryLabel(category === 'Autre' ? null : category) }}
          <span class="category-count">{{ count }}</span>
        </button>

        <button v-if="selectedCategory" type="button" class="clear-btn" @click="selectedCategory = null">
          Effacer le filtre
        </button>
      </nav>
    </div>

    <div v-if="store.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ store.error }}
    </div>

    <div v-else-if="store.loading" class="status">
      <span class="spinner" />
      Chargement des objets…
    </div>

    <template v-else>
      <div class="item-grid">
        <div v-for="item in visibleItems" :key="item.id" class="item-card">
          <span v-if="item.soldByMerchant" class="badge-merchant" title="Vendu par un marchand">
            <i class="mdi mdi-store" />
          </span>

          <button
            type="button"
            class="item-btn"
            @click="selectedItem = item"
          >
            <img v-if="item.iconUrl" :src="item.iconUrl" :alt="item.name" width="56" height="56" loading="lazy">
            <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>

            <span class="item-name">{{ item.name }}</span>

            <span v-if="item.price !== null" class="item-prices">
              <span class="price-buy" title="Prix d'achat">{{ formatNumber(item.price) }}</span>
              <span class="price-sell" title="Prix de vente estimé (10% du prix d'achat)">
                <i class="mdi mdi-cash-minus" />
                {{ formatNumber(sellPrice(item)) }}
              </span>
            </span>
          </button>
        </div>
      </div>

      <p v-if="visibleItems.length === 0" class="empty">Aucun objet ne correspond à la recherche.</p>
    </template>

    <ItemSellSimulationModal
      v-if="selectedItem"
      :item="selectedItem"
      @close="selectedItem = null"
    />
  </div>
</template>

<style lang="scss" scoped>
.items-page {
  padding: 1.5rem;
  max-width: 1500px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

@media (max-width: 640px) {
  .items-page { padding: 1rem; }
}

.items-header {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.items-toolbar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.search-input {
  margin: 0;
  height: 2rem;
  font-size: 0.75rem;
  width: 240px;
}

.items-count {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--pico-muted-color);

  strong { color: var(--pico-primary); font-weight: 700; }
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.sort-select {
  margin: 0;
  height: 2rem;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
  width: auto;
  min-width: 130px;
}

.sort-order-btn {
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

.category-tabs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.category-tab {
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

.category-count {
  font-size: 0.68rem;
  color: var(--pico-muted-color);
}

.category-tab.active .category-count {
  color: var(--pico-primary);
}

.clear-btn {
  padding: 0.35rem 0.8rem;
  font-size: 0.75rem;
  width: auto;
  margin: 0;
  border-radius: 6px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-contrast);
  }
}

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

.item-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: 1px;
  background: var(--pico-card-border-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  overflow: hidden;
}

.item-card {
  position: relative;
  background: var(--pico-card-background-color);
}

.item-btn {
  width: 100%;
  margin: 0;
  padding: 0.85rem 0.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background 0.15s ease;

  &:hover {
    background: color-mix(in srgb, var(--pico-primary) 6%, transparent);
  }

  img, .icon-placeholder {
    width: 56px;
    height: 56px;
    border-radius: 6px;
    object-fit: cover;
  }

  .icon-placeholder {
    display: grid;
    place-items: center;
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
    color: var(--pico-muted-color);
    font-size: 1.3rem;
  }
}

.item-name {
  font-size: 0.78rem;
  text-align: center;
  line-height: 1.25;
  color: var(--pico-color);
  min-height: 2rem;
}

.item-prices {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.05rem;
}

.price-buy {
  font-weight: 700;
  font-size: 0.82rem;
  color: #d4af37;
}

.price-sell {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  font-size: 0.68rem;
  color: var(--pico-muted-color);

  i { font-size: 0.6rem; }
}

.badge-merchant {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.3rem;
  height: 1.3rem;
  border-radius: 50%;
  background: color-mix(in srgb, var(--pico-primary) 18%, transparent);
  color: var(--pico-primary);
  font-size: 0.7rem;
}
</style>
