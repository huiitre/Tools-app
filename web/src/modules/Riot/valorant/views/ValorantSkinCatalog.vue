<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch, nextTick } from 'vue'
import { clientV3 } from '@/services/axiosInstance'
import { fetchWeapons } from '@/modules/Riot/valorant/fetch/valorantShop.fetch'
import { fetchMySkins, fetchWatchlist } from '@/modules/Riot/valorant/fetch/valorantUserSkins.fetch'
import { useRiotStore } from '@/modules/Riot/riot.store'
import { useValorantSkinDetail } from '../composables/useValorantSkinDetail'
import type { ValorantSkin, ValorantWeapon } from '../valorant.types'
import ValorantSkinCard from '../components/ValorantSkinCard.vue'

const STORAGE_KEY_FILTERS = 'riot.valorant.catalog.filters'

type FilterState = 'all' | 'owned' | 'watched' | 'unowned'
type SortBy = 'name' | 'id' | 'rank'
type SortDir = 'asc' | 'desc'

const riotStore = useRiotStore()
const { open: openSkinDetail } = useValorantSkinDetail()
const skins = ref<ValorantSkin[]>([])
const weapons = ref<ValorantWeapon[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

// Filters & Sort
const q = ref('')
const weaponId = ref<number | null>(null)
const stateFilter = ref<FilterState>('all')
const sortBy = ref<SortBy>('name')
const sortDir = ref<SortDir>('asc')
const activeTierUuid = ref<string | null>(null)
const contentTierFilter = ref<string | null>(null)

const isAnyFilterActive = computed(() => {
  return q.value.trim() !== '' ||
    weaponId.value !== null ||
    stateFilter.value !== 'all' ||
    sortBy.value !== 'name' ||
    sortDir.value !== 'asc' ||
    activeTierUuid.value !== null ||
    contentTierFilter.value !== null
})

const availableContentTiers = computed(() => {
  const seen = new Map<string, { assetId: string; name: string; rank: number }>()
  for (const skin of skins.value) {
    if (skin.contentTier && !seen.has(skin.contentTier.assetId)) {
      seen.set(skin.contentTier.assetId, {
        assetId: skin.contentTier.assetId,
        name: skin.contentTier.name,
        rank: skin.contentTier.rank,
      })
    }
  }
  return [...seen.values()].sort((a, b) => b.rank - a.rank)
})

// Progressive Rendering
const PAGE_SIZE = 48
const limit = ref(PAGE_SIZE)
const sentinel = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null

// Back to Top
const showBackToTop = ref(false)

const filteredSkins = computed(() => {
  let result = [...skins.value]

  // Collection Filter (Special State)
  if (activeTierUuid.value) {
    result = result.filter(s => s.tierUuid === activeTierUuid.value)
    return result
  }

  // Search
  if (q.value.trim()) {
    const search = q.value.toLowerCase().trim()
    result = result.filter(s => s.name.toLowerCase().includes(search))
  }

  // Weapon Filter
  if (weaponId.value !== null) {
    result = result.filter(s => s.weaponId === weaponId.value)
  }

  // State Filter
  if (stateFilter.value === 'owned') {
    result = result.filter(s => riotStore.isSkinOwned(s.id))
  } else if (stateFilter.value === 'watched') {
    result = result.filter(s => riotStore.isSkinWatched(s.id))
  } else if (stateFilter.value === 'unowned') {
    result = result.filter(s => !riotStore.isSkinOwned(s.id))
  }

  // Content Tier Filter
  if (contentTierFilter.value !== null) {
    result = result.filter(s => s.contentTier?.assetId === contentTierFilter.value)
  }

  // Sort
  result.sort((a, b) => {
    if (sortBy.value === 'name') {
      const cmp = a.name.localeCompare(b.name, 'fr', { sensitivity: 'base' })
      return sortDir.value === 'asc' ? cmp : -cmp
    } else if (sortBy.value === 'id') {
      const diff = a.id - b.id
      return sortDir.value === 'asc' ? diff : -diff
    } else if (sortBy.value === 'rank') {
      const noRank = sortDir.value === 'asc' ? Infinity : -Infinity
      const rankA = a.contentTier?.rank ?? noRank
      const rankB = b.contentTier?.rank ?? noRank
      return sortDir.value === 'asc' ? rankA - rankB : rankB - rankA
    }
    return 0
  })

  return result
})

const displayedSkins = computed(() => filteredSkins.value.slice(0, limit.value))
const hasMore = computed(() => limit.value < filteredSkins.value.length)

function toggleSortDir() {
  sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
}

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function handleScroll() {
  showBackToTop.value = window.scrollY > 600
}

function clearFilters() {
  q.value = ''
  weaponId.value = null
  stateFilter.value = 'all'
  sortBy.value = 'name'
  sortDir.value = 'asc'
  activeTierUuid.value = null
  contentTierFilter.value = null
  localStorage.removeItem(STORAGE_KEY_FILTERS)
}

function clearCollectionFilter() {
  activeTierUuid.value = null
  scrollToTop()
}

function setCollectionFilter(uuid: string) {
  activeTierUuid.value = uuid
  scrollToTop()
}

// Reset limit on filter/sort change
watch([q, weaponId, stateFilter, sortBy, sortDir, activeTierUuid, contentTierFilter], () => {
  limit.value = PAGE_SIZE
  scrollToTop()
})

// Persist filters to localStorage
watch([weaponId, stateFilter, sortBy, sortDir, contentTierFilter], () => {
  const data = {
    weaponId: weaponId.value,
    stateFilter: stateFilter.value,
    sortBy: sortBy.value,
    sortDir: sortDir.value,
    contentTierFilter: contentTierFilter.value,
  }
  localStorage.setItem(STORAGE_KEY_FILTERS, JSON.stringify(data))
})

function hydrateFilters() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_FILTERS)
    if (!raw) return
    const data = JSON.parse(raw)
    
    if (data.weaponId !== undefined) weaponId.value = data.weaponId
    if (data.stateFilter) stateFilter.value = data.stateFilter
    if (data.sortBy) sortBy.value = data.sortBy
    if (data.sortDir) sortDir.value = data.sortDir
    if (data.contentTierFilter !== undefined) contentTierFilter.value = data.contentTierFilter
  } catch (e) {
    console.warn('Failed to hydrate Valorant filters', e)
  }
}

function initObserver() {
  if (observer) observer.disconnect()

  observer = new IntersectionObserver((entries) => {
    if (entries[0].isIntersecting && hasMore.value) {
      limit.value += PAGE_SIZE
    }
  }, { rootMargin: '400px' })

  if (sentinel.value) observer.observe(sentinel.value)
}

async function loadSkins() {
  try {
    riotStore.clearAccountSession()
    const { data } = await clientV3.get<ValorantSkin[]>('/riot/valorant/skins', {
      params: { accountId: riotStore.selectedAccountId ?? undefined },
    })
    skins.value = data
    riotStore.syncFromSkins(data)
    error.value = null
  } catch {
    error.value = 'Impossible de charger les skins.'
  }
}

watch(() => riotStore.selectedAccountId, () => {
  if (!loading.value) loadSkins()
})

onMounted(async () => {
  hydrateFilters()
  window.addEventListener('scroll', handleScroll, { passive: true })
  try {
    const [, weaponsRes] = await Promise.all([
      loadSkins(),
      fetchWeapons()
    ])
    weapons.value = weaponsRes.sort((a, b) => a.name.localeCompare(b.name))

    await nextTick()
    initObserver()
  } catch {
    error.value = 'Impossible de charger les skins.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  if (observer) observer.disconnect()
})
</script>

<template>
  <div class="catalog-wrapper">

    <!-- ── TOOLBAR ───────────────────────────────────────────── -->
    <div class="catalog-toolbar">
      <input
        v-model="q"
        type="search"
        placeholder="Rechercher un skin..."
        class="search-input"
        :disabled="!!activeTierUuid"
      />

      <select v-model="weaponId" class="toolbar-select" :disabled="!!activeTierUuid">
        <option :value="null">Toutes les armes</option>
        <option v-for="w in weapons" :key="w.id" :value="w.id">
          {{ w.name }}
        </option>
      </select>

      <select v-model="stateFilter" class="toolbar-select state-select" :disabled="!!activeTierUuid">
        <option value="all">Tous les skins</option>
        <option value="owned">Obtenus</option>
        <option value="watched">Surveillés</option>
        <option value="unowned">Non possédés</option>
      </select>

      <select v-model="contentTierFilter" class="toolbar-select" :disabled="!!activeTierUuid">
        <option :value="null">Toutes les raretés</option>
        <option v-for="tier in availableContentTiers" :key="tier.assetId" :value="tier.assetId">
          {{ tier.name }}
        </option>
      </select>

      <div class="sort-controls">
        <select v-model="sortBy" class="toolbar-select sort-select" :disabled="!!activeTierUuid">
          <option value="name">Nom</option>
          <option value="id">ID</option>
          <option value="rank">Rareté</option>
        </select>

        <button
          type="button"
          class="toolbar-btn sort-order-btn"
          @click="toggleSortDir"
          :title="sortDir === 'asc' ? 'Croissant' : 'Décroissant'"
          :disabled="!!activeTierUuid"
        >
          <i class="mdi" :class="sortDir === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" />
        </button>
      </div>

      <button
        type="button"
        class="toolbar-btn reset-btn"
        title="Réinitialiser les filtres"
        @click="clearFilters"
        :disabled="!isAnyFilterActive"
      >
        <i class="mdi mdi-filter-remove-outline" />
      </button>

      <div v-if="activeTierUuid" class="collection-filter-tag">
        <i class="mdi mdi-layers-triple" />
        <span>Collection active</span>
        <button class="clear-tag-btn" @click="clearCollectionFilter">
          <i class="mdi mdi-close" />
        </button>
      </div>

      <span v-if="!loading && !error" class="catalog-count">
        <span class="count-sep" />
        <strong>{{ filteredSkins.length.toLocaleString('fr-FR') }}</strong>
        / {{ skins.length.toLocaleString('fr-FR') }} skins
      </span>
    </div>

    <!-- ── ERROR ─────────────────────────────────────────────── -->
    <div v-if="error" class="catalog-error">
      <i class="mdi mdi-alert-circle-outline" />
      {{ error }}
    </div>

    <!-- ── SKELETON ──────────────────────────────────────────── -->
    <div v-else-if="loading" class="skin-grid">
      <div v-for="i in 24" :key="i" class="skin-card-skeleton">
        <div class="skeleton-image" />
        <div class="skeleton-line" style="width: 70%; height: 0.8rem; margin: 0.6rem auto;" />
      </div>
    </div>

    <!-- ── GRID ──────────────────────────────────────────────── -->
    <div v-else class="skin-grid">
      <ValorantSkinCard
        v-for="skin in displayedSkins"
        :key="skin.id"
        :skin="skin"
        @detail="openSkinDetail"
        @filter-tier="setCollectionFilter"
      />
    </div>

    <!-- ── SENTINEL (Intersection Observer) ──────────────────── -->
    <div ref="sentinel" class="sentinel">
      <div v-if="hasMore" class="loading-more">
        <div class="spinner" />
      </div>
    </div>

    <!-- ── EMPTY ─────────────────────────────────────────────── -->
    <div v-if="!loading && !error && filteredSkins.length === 0" class="catalog-empty">
      <i class="mdi mdi-magnify-close" />
      Aucun skin ne correspond à vos critères.
    </div>

    <!-- ── BACK TO TOP ────────────────────────────────────────── -->
    <Transition name="fade">
      <button
        v-if="showBackToTop"
        class="back-to-top"
        title="Retour en haut"
        @click="scrollToTop"
      >
        <i class="mdi mdi-chevron-up" />
      </button>
    </Transition>

  </div>
</template>

<style lang="scss" scoped>
.catalog-wrapper {
  padding: 1.5rem;
  max-width: 1400px;
  margin: 0 auto;
}

/* ── Toolbar ────────────────────────────────────────────────────────────── */
.catalog-toolbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
}

.search-input {
  margin: 0;
  height: 2rem;
  font-size: 0.75rem;
  width: 200px;
}

.toolbar-select {
  margin: 0;
  height: 2rem;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
  width: auto;

  &.state-select { min-width: 130px; }
  &.sort-select { min-width: 110px; }
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

  &:hover:not(:disabled) {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    filter: grayscale(1);
  }

  i { font-size: 1rem; }
}

.reset-btn:hover:not(:disabled) {
  border-color: var(--pico-del-color);
  color: var(--pico-del-color);
}

.collection-filter-tag {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0 0.75rem;
  height: 2rem;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  border: 1px solid var(--pico-primary);
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--pico-primary);

  .clear-tag-btn {
    background: transparent;
    border: none;
    padding: 0;
    margin: 0;
    width: auto;
    height: auto;
    color: inherit;
    cursor: pointer;
    display: flex;
    align-items: center;

    &:hover { color: var(--pico-color); }
  }
}

.catalog-count {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
  margin-left: auto;

  strong {
    color: var(--pico-primary);
    font-weight: 700;
  }
}

.count-sep {
  display: inline-block;
  width: 1px;
  height: 1rem;
  background: var(--pico-muted-border-color);
  margin-right: 0.15rem;
}

/* ── States ─────────────────────────────────────────────────────────────── */
.catalog-error, .catalog-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 5rem 2rem;
  color: var(--pico-muted-color);
  font-size: 1rem;

  i { font-size: 3rem; opacity: 0.5; }
}

.catalog-error { color: var(--pico-del-color); }

/* ── Grid ───────────────────────────────────────────────────────────────── */
.skin-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1.25rem;
}

.skin-card-skeleton {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  overflow: hidden;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

%shimmer {
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

.skeleton-image {
  @extend %shimmer;
  aspect-ratio: 1 / 1;
}

.skeleton-line {
  @extend %shimmer;
  display: block;
  border-radius: 6px;
}

/* ── Sentinel & Infinite Scroll ─────────────────────────────────────────── */
.sentinel {
  min-height: 60px;
  display: flex;
  justify-content: center;
  align-items: center;
}

.spinner {
  width: 1.25rem;
  height: 1.25rem;
  border: 2px solid var(--pico-muted-border-color);
  border-top-color: var(--pico-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ── Back to Top ───────────────────────────────────────────────────────── */
.back-to-top {
  position: fixed;
  bottom: 6rem;
  right: 2rem;
  width: 3rem;
  height: 3rem;
  border-radius: 50%;
  background: var(--pico-primary-background);
  color: var(--pico-primary-inverse);
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  z-index: 100;
  transition: transform 0.2s, background-color 0.2s;

  &:hover {
    background: var(--pico-primary-hover);
    transform: scale(1.1);
  }

  i { font-size: 1.5rem; }
}

.fade-enter-active, .fade-leave-active { transition: opacity 0.3s, transform 0.3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; transform: translateY(20px); }

</style>
