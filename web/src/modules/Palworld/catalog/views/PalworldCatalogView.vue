<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import { normalizeSearchText } from '@/utils/searchNormalize'
import { useItemsStore } from '../items.store'
import { usePaldexStore } from '../../paldex/paldex.store'
import { categoryLabel } from '../utils/categoryLabel'
import ItemSellSimulationModal from '../components/ItemSellSimulationModal.vue'

interface CatalogEntry {
  key: string
  kind: 'item' | 'pal'
  name: string
  iconUrl: string | null
  category: string
  buyPrice: number | null
  sellPrice: number | null
  soldByMerchant: boolean
}

type SortKey = 'category' | 'name' | 'price'

const SORT_OPTIONS: { id: SortKey; label: string }[] = [
  { id: 'category', label: 'Catégorie' },
  { id: 'name', label: 'Nom' },
  { id: 'price', label: 'Prix' },
]

const itemsStore = useItemsStore()
const paldexStore = usePaldexStore()
const searchQuery = ref('')
const selectedCategory = ref<string | null>(null)
const sortKey = ref<SortKey>('category')
const sortDir = ref<'asc' | 'desc'>('asc')
const selectedEntry = ref<CatalogEntry | null>(null)

const normalizedQuery = computed(() => normalizeSearchText(searchQuery.value.trim()))

// Fusion items (achat + vente estimée 10%) et Pals (vente uniquement, pal.price = déjà un prix de vente
// réel côté jeu, pas d'achat en or) dans un seul catalogue parcourable/triable.
const allEntries = computed<CatalogEntry[]>(() => [
  ...itemsStore.items.map((item): CatalogEntry => ({
    key: `item:${item.id}`,
    kind: 'item',
    name: item.name,
    iconUrl: item.iconUrl,
    category: item.category ?? 'Autre',
    buyPrice: item.price,
    sellPrice: item.price !== null ? Math.floor(item.price * 0.1) : null,
    soldByMerchant: item.soldByMerchant,
  })),
  ...paldexStore.pals.map((pal): CatalogEntry => ({
    key: `pal:${pal.id}`,
    kind: 'pal',
    name: pal.name,
    iconUrl: pal.imageUrl,
    category: 'Pals',
    buyPrice: null,
    sellPrice: pal.price,
    soldByMerchant: false,
  })),
])

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function sortValue(entry: CatalogEntry): number | string {
  switch (sortKey.value) {
    case 'name': return entry.name.toLowerCase()
    case 'price': return entry.sellPrice ?? entry.buyPrice ?? -1
    default: return entry.category.toLowerCase() + '|' + entry.name.toLowerCase()
  }
}

const availableCategories = computed(() => {
  const byCategory = new Map<string, number>()
  for (const entry of allEntries.value) {
    byCategory.set(entry.category, (byCategory.get(entry.category) ?? 0) + 1)
  }
  return [...byCategory.entries()].sort((a, b) => b[1] - a[1])
})

function matchesSearch(entry: CatalogEntry): boolean {
  return !normalizedQuery.value || normalizeSearchText(entry.name).includes(normalizedQuery.value)
}

function matchesCategory(entry: CatalogEntry): boolean {
  return !selectedCategory.value || entry.category === selectedCategory.value
}

const visibleEntries = computed(() => {
  const filtered = allEntries.value.filter(entry => matchesSearch(entry) && matchesCategory(entry))
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

onMounted(() => {
  itemsStore.ensureLoaded()
  paldexStore.ensureLoaded()
})
</script>

<template>
  <div class="items-page">
    <div class="items-header">
      <div class="items-toolbar">
        <input
          v-model="searchQuery"
          type="search"
          placeholder="Rechercher un objet ou un Pal..."
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
          <strong>{{ visibleEntries.length }}</strong> / {{ allEntries.length }}
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

    <div v-if="itemsStore.error || paldexStore.error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ itemsStore.error || paldexStore.error }}
    </div>

    <div v-else-if="itemsStore.loading || paldexStore.loading" class="status">
      <span class="spinner" />
      Chargement du catalogue…
    </div>

    <template v-else>
      <div class="item-grid">
        <div v-for="entry in visibleEntries" :key="entry.key" class="item-card">
          <span v-if="entry.soldByMerchant" class="badge-merchant" title="Vendu par un marchand">
            <i class="mdi mdi-store" />
          </span>

          <button
            type="button"
            class="item-btn"
            @click="selectedEntry = entry"
          >
            <img v-if="entry.iconUrl" :src="entry.iconUrl" :alt="entry.name" width="56" height="56" loading="lazy">
            <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>

            <span class="item-name">{{ entry.name }}</span>

            <span class="item-prices">
              <span class="price-buy" :title="entry.buyPrice !== null ? `Prix d'achat` : `Prix d'achat non renseigné`">
                {{ entry.buyPrice !== null ? formatNumber(entry.buyPrice) : '0' }}
              </span>
              <span
                v-if="entry.sellPrice !== null"
                class="price-sell"
                :title="entry.kind === 'pal' ? 'Prix de vente' : `Prix de vente estimé (10% du prix d'achat)`"
              >
                <i class="mdi mdi-cash-minus" />
                {{ formatNumber(entry.sellPrice) }}
              </span>
            </span>
          </button>
        </div>
      </div>

      <p v-if="visibleEntries.length === 0" class="empty">Rien ne correspond à la recherche.</p>
    </template>

    <ItemSellSimulationModal
      v-if="selectedEntry"
      :name="selectedEntry.name"
      :icon-url="selectedEntry.iconUrl"
      :unit-sell-price="selectedEntry.sellPrice ?? 0"
      @close="selectedEntry = null"
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
