<script setup lang="ts">
import { computed, ref, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { usePaldexStore } from '../../paldex/paldex.store'
import { usePassiveSkillsStore } from '../../passives/passiveSkills.store'
import { usePalworldServerDataStore } from '../../server/serverData.store'
import BreedingPassiveSelectorModal from '../../breeding/components/BreedingPassiveSelectorModal.vue'
import type { PalworldPalListItem, PalworldWorkSuitabilitySummary } from '../../paldex/types/paldex.types'
import type { PalworldServerPalInventory, PalworldPalStorageLocation } from '../../server/types/palworldServerData.types'

type SortKey = 'name' | 'paldex' | 'hp' | 'level' | 'workSpeed'
type IvSortKey = 'hp' | 'attack' | 'defense'
interface LocationOption { key: string; label: string; icon: string }

interface InventoryPal extends PalworldServerPalInventory {
  catalog: PalworldPalListItem
  ownerName: string | null
  baseLabel: string | null
}

const paldexStore = usePaldexStore()
const passiveSkillsStore = usePassiveSkillsStore()
const serverDataStore = usePalworldServerDataStore()

const searchQuery = ref('')
const selectedElementIds = ref<Set<number>>(new Set())
const selectedWorkSuitabilityIds = ref<Set<number>>(new Set())
const selectedPassiveIds = ref<Set<string>>(new Set())
const selectedFavoriteLevels = ref<Set<number>>(new Set())
const selectedRanks = ref<Set<number>>(new Set())
const selectedIvSortKeys = ref<Set<IvSortKey>>(new Set())
const selectedLocationKeys = ref<Set<string>>(new Set())
const pendingLocationKeys = ref<Set<string>>(new Set())
const locationSelectorOpen = ref(false)
const locationFilterInitialized = ref(false)
const requireAllPassives = ref(false)
const passiveSelectorOpen = ref(false)
const sortKey = ref<SortKey>('name')
const sortDirection = ref<'asc' | 'desc'>('asc')
const ivSortDirection = ref<'asc' | 'desc'>('desc')
const ivSortOnAverage = ref(false)
const snapshotWorkKeyBySlug: Record<string, string> = {
  Kindling: 'EmitFlame', Watering: 'Watering', Planting: 'Seeding', Generating_Electricity: 'GenerateElectricity',
  Handiwork: 'Handcraft', Gathering: 'Collection', Lumbering: 'Deforest', Mining: 'Mining',
  Medicine_Production: 'ProductMedicine', Cooling: 'Cool', Transporting: 'Transport', Farming: 'MonsterFarm',
}
const workSuitabilityOrder = ['EmitFlame', 'Watering', 'Seeding', 'GenerateElectricity', 'Handcraft', 'Collection', 'Deforest', 'Mining', 'OilExtraction', 'ProductMedicine', 'Cool', 'Transport', 'MonsterFarm']

const normalizedQuery = computed(() => searchQuery.value.trim().toLocaleLowerCase())
const catalogById = computed(() => new Map(paldexStore.pals.map(pal => [pal.id, pal])))
const passiveById = computed(() => new Map(passiveSkillsStore.passiveSkills.map(passive => [passive.id, passive])))
const ownerNameById = computed(() => new Map(
  serverDataStore.guilds.flatMap(guild => guild.players.map(player => [player.playerUid, player.name] as const)),
))
const baseLabelById = computed(() => new Map(
  serverDataStore.guilds.flatMap(guild => guild.bases.map(base => [base.baseId, `Base ${base.baseId.slice(0, 8)}`] as const)),
))
const selectedPlayerName = computed(() => {
  const id = serverDataStore.selectedPlayerIds[0]
  return id ? ownerNameById.value.get(id) ?? null : null
})
const guildNameByBaseId = computed(() => new Map(
  serverDataStore.guilds.flatMap(guild => guild.bases.map(base => [base.baseId, guild.name] as const)),
))

// Sélection dans la navigation : un joueur choisi limite l'inventaire à ses Pals.
// Sans joueur choisi, l'écran reste un inventaire global du serveur.
const sourcePals = computed(() => serverDataStore.selectedPlayerIds.length
  ? serverDataStore.selectedPals
  : serverDataStore.pals)

const inventoryPals = computed<InventoryPal[]>(() => sourcePals.value.flatMap(pal => {
  if (pal.palId === null) return []
  const catalog = catalogById.value.get(pal.palId)
  if (!catalog) return []
  return [{
    ...pal,
    catalog,
    ownerName: pal.ownerPlayerUid
      ? ownerNameById.value.get(pal.ownerPlayerUid) ?? null
      : pal.baseId ? selectedPlayerName.value ?? guildNameByBaseId.value.get(pal.baseId) ?? null : null,
    baseLabel: pal.baseId ? baseLabelById.value.get(pal.baseId) ?? `Base ${pal.baseId.slice(0, 8)}` : null,
  }]
}))

const availableElements = computed(() => {
  const byId = new Map<number, { id: number; name: string; iconUrl: string | null }>()
  for (const pal of inventoryPals.value) for (const element of pal.catalog.elements) byId.set(element.id, element)
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})
const availableWorkSuitabilities = computed(() => {
  const byId = new Map<number, PalworldWorkSuitabilitySummary>()
  for (const pal of inventoryPals.value) for (const work of pal.catalog.workSuitabilities) byId.set(work.id, work)
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
})
const availablePassiveIds = computed(() => new Set(inventoryPals.value.flatMap(pal => pal.passiveSkillIds)))
const locationOptions = computed<LocationOption[]>(() => {
  const options: LocationOption[] = []
  const nonBaseLocations: { location: Exclude<PalworldPalStorageLocation, 'base'>; icon: string }[] = [
    { location: 'palbox', icon: 'mdi-archive' },
    { location: 'dimensional_storage', icon: 'mdi-archive-outline' },
    { location: 'party', icon: 'mdi-account-group' },
  ]
  for (const { location, icon } of nonBaseLocations) {
    if (inventoryPals.value.some(pal => pal.storageLocation === location)) options.push({ key: `storage:${location}`, label: storageLabel(location), icon })
  }
  const baseIds = [...new Set(inventoryPals.value.flatMap(pal => pal.baseId ? [pal.baseId] : []))].sort()
  for (const baseId of baseIds) options.push({ key: `base:${baseId}`, label: baseLabelById.value.get(baseId) ?? `Base ${baseId.slice(0, 8)}`, icon: 'mdi-home-variant' })
  return options
})
watch(locationOptions, options => {
  if (!locationFilterInitialized.value && options.length) {
    selectedLocationKeys.value = new Set(options.map(option => option.key))
    locationFilterInitialized.value = true
  }
}, { immediate: true })
watch(() => serverDataStore.selectedPlayerIds.join(','), () => { locationFilterInitialized.value = false })

function toggleSet(source: Set<number>, id: number): Set<number> {
  const next = new Set(source)
  next.has(id) ? next.delete(id) : next.add(id)
  return next
}
function toggleElement(id: number) { selectedElementIds.value = toggleSet(selectedElementIds.value, id) }
function toggleWorkSuitability(id: number) { selectedWorkSuitabilityIds.value = toggleSet(selectedWorkSuitabilityIds.value, id) }
function toggleFavorite(id: number) { selectedFavoriteLevels.value = toggleSet(selectedFavoriteLevels.value, id) }
function toggleRank(id: number) { selectedRanks.value = toggleSet(selectedRanks.value, id) }
function toggleIvSort(key: IvSortKey) {
  const next = new Set(selectedIvSortKeys.value)
  next.has(key) ? next.delete(key) : next.add(key)
  selectedIvSortKeys.value = next
}
function togglePassive(id: string) {
  const next = new Set(selectedPassiveIds.value)
  next.has(id) ? next.delete(id) : next.add(id)
  selectedPassiveIds.value = next
}
function applyPassiveSelection(ids: string[]) {
  selectedPassiveIds.value = new Set(ids)
  if (ids.length > 4) requireAllPassives.value = false
  passiveSelectorOpen.value = false
}
function openLocationSelector() { pendingLocationKeys.value = new Set(selectedLocationKeys.value); locationSelectorOpen.value = true }
function togglePendingLocation(key: string) { const next = new Set(pendingLocationKeys.value); next.has(key) ? next.delete(key) : next.add(key); pendingLocationKeys.value = next }
function applyLocationSelection() { selectedLocationKeys.value = new Set(pendingLocationKeys.value); locationSelectorOpen.value = false }
watch(selectedPassiveIds, ids => { if (ids.size > 4) requireAllPassives.value = false })

function matchesFilters(pal: InventoryPal) {
  const nameMatches = !normalizedQuery.value || pal.catalog.name.toLocaleLowerCase().includes(normalizedQuery.value)
    || String(pal.catalog.paldexIndex ?? '').includes(normalizedQuery.value)
  const elementMatches = !selectedElementIds.value.size || pal.catalog.elements.some(element => selectedElementIds.value.has(element.id))
  const workMatches = !selectedWorkSuitabilityIds.value.size || pal.catalog.workSuitabilities.some(work => selectedWorkSuitabilityIds.value.has(work.id))
  const passiveMatches = !selectedPassiveIds.value.size || (requireAllPassives.value
    ? [...selectedPassiveIds.value].every(id => pal.passiveSkillIds.includes(id))
    : pal.passiveSkillIds.some(id => selectedPassiveIds.value.has(id)))
  const locationKey = pal.baseId ? `base:${pal.baseId}` : `storage:${pal.storageLocation}`
  const rankMatches = !selectedRanks.value.size || selectedRanks.value.has(pal.rank)
  return nameMatches && elementMatches && workMatches && passiveMatches && rankMatches && selectedLocationKeys.value.has(locationKey)
}
function sortValue(pal: InventoryPal): string | number {
  switch (sortKey.value) {
    case 'paldex': return pal.catalog.paldexIndex ?? Number.MAX_SAFE_INTEGER
    case 'hp': return pal.currentHp ?? -1
    case 'level': return pal.level ?? -1
    case 'workSpeed': {
      const levels = pal.catalog.workSuitabilities
        .filter(work => selectedWorkSuitabilityIds.value.has(work.id))
        .map(work => workLevel(pal, work))
      return levels.length ? Math.max(...levels) : Math.max(...pal.catalog.workSuitabilities.map(work => workLevel(pal, work)), -1)
    }
    default: return pal.catalog.name.toLocaleLowerCase()
  }
}
// Mode « moyenne » : la moyenne des IV cochées prime, puis la plus faible départage — un Pal équilibré
// passe devant un Pal qui n'a qu'une seule stat très haute (ce que fait le mode par défaut, basé sur max).
const ivSortCriteria = computed<Array<'perfect' | 'max' | 'average' | 'min'>>(
  () => ivSortOnAverage.value ? ['average', 'min', 'max'] : ['perfect', 'max', 'average', 'min'],
)
const visiblePals = computed(() => [...inventoryPals.value].filter(matchesFilters).sort((a, b) => {
  const favoriteOrder = [...selectedFavoriteLevels.value].sort((left, right) => left - right)
  if (favoriteOrder.length) {
    const aPosition = favoriteOrder.indexOf(favoriteLevel(a.favoriteIndex))
    const bPosition = favoriteOrder.indexOf(favoriteLevel(b.favoriteIndex))
    const aPriority = aPosition === -1 ? favoriteOrder.length : aPosition
    const bPriority = bPosition === -1 ? favoriteOrder.length : bPosition
    if (aPriority !== bPriority) return aPriority - bPriority
  }
  if (selectedIvSortKeys.value.size) {
    const keys = ['hp', 'attack', 'defense'] as IvSortKey[]
    const values = (pal: InventoryPal) => keys
      .filter(key => selectedIvSortKeys.value.has(key))
      .map(key => ivValue(key === 'hp' ? pal.ivHp : key === 'attack' ? pal.ivAttack : pal.ivDefense))
    const aValues = values(a); const bValues = values(b)
    const score = (ivValues: number[]) => ({
      perfect: ivValues.filter(value => value >= 100).length,
      max: Math.max(...ivValues),
      average: ivValues.reduce((sum, value) => sum + value, 0) / ivValues.length,
      min: Math.min(...ivValues),
    })
    const aScore = score(aValues); const bScore = score(bValues)
    for (const key of ivSortCriteria.value) {
      const comparison = aScore[key] < bScore[key] ? -1 : aScore[key] > bScore[key] ? 1 : 0
      if (comparison !== 0) return ivSortDirection.value === 'asc' ? comparison : -comparison
    }
  }
  const va = sortValue(a); const vb = sortValue(b)
  const comparison = va < vb ? -1 : va > vb ? 1 : 0
  return sortDirection.value === 'asc' ? comparison : -comparison
}))

// Rendu progressif : jusqu'à ~2000 Pals possédés, chaque carte porte beaucoup de
// markup (IV, passifs, aptitudes calculées) — tout monter d'un coup fait ramer le
// scroll et les changements de filtre. Même pattern que ValorantSkinCatalog.vue /
// PaldexView.vue : on affiche par lots, un IntersectionObserver charge la suite.
const INVENTORY_PAGE_SIZE = 60
const inventoryLimit = ref(INVENTORY_PAGE_SIZE)
const inventorySentinel = ref<HTMLElement | null>(null)
let inventoryObserver: IntersectionObserver | null = null

const displayedPals = computed(() => visiblePals.value.slice(0, inventoryLimit.value))
const hasMoreInventoryPals = computed(() => inventoryLimit.value < visiblePals.value.length)

function initInventoryObserver() {
  if (inventoryObserver) inventoryObserver.disconnect()

  inventoryObserver = new IntersectionObserver((entries) => {
    if (entries[0].isIntersecting && hasMoreInventoryPals.value) {
      inventoryLimit.value += INVENTORY_PAGE_SIZE
    }
  }, { rootMargin: '400px' })

  if (inventorySentinel.value) inventoryObserver.observe(inventorySentinel.value)
}

// visiblePals change de référence à chaque filtre/tri/sélection joueur : on
// réinitialise la limite dessus plutôt que de lister chaque ref individuellement.
watch(visiblePals, () => { inventoryLimit.value = INVENTORY_PAGE_SIZE })

async function trySetupInventoryObserver() {
  if (serverDataStore.loading || paldexStore.loading || !inventoryPals.value.length) return
  await nextTick()
  initInventoryObserver()
}

watch(() => serverDataStore.loading || paldexStore.loading, trySetupInventoryObserver)

onMounted(() => {
  trySetupInventoryObserver()
})

onUnmounted(() => {
  if (inventoryObserver) inventoryObserver.disconnect()
})

function storageLabel(location: PalworldPalStorageLocation) {
  return { base: 'En base', palbox: 'Palbox', dimensional_storage: 'Boîte dimensionnelle', party: 'Équipe' }[location]
}
// favorite_index est le niveau du cadenas : 0 et null signifient « aucun cadenas ».
function favoriteLevel(index: number | null) { return index === null || index <= 0 ? 0 : Math.min(index, 3) }
// Un IV absent du snapshot vaut 0 dans le jeu, pas « inconnu ».
function ivValue(value: number | null) { return value ?? 0 }
function ivClass(value: number) {
  if (value >= 100) return 'iv-perfect'
  if (value > 70) return 'iv-high'
  return 'iv-normal'
}
function workSnapshotKey(pal: InventoryPal, work: PalworldWorkSuitabilitySummary) {
  const candidates = [work.slug, snapshotWorkKeyBySlug[work.slug], work.name].filter(Boolean).map(value => value.toLowerCase().replace(/[^a-z0-9]/g, ''))
  return Object.keys(pal.baseWorkSuitability).find(key => candidates.includes(key.toLowerCase().replace(/[^a-z0-9]/g, ''))) ?? work.slug
}
function workLevel(pal: InventoryPal, work: PalworldWorkSuitabilitySummary) {
  const key = workSnapshotKey(pal, work)
  return Math.min(10, (pal.baseWorkSuitability[key] ?? work.level) + workCondensationBonus(pal, work) + (pal.workSuitabilityAddRanks[key] ?? 0))
}
function workCondensationBonuses(pal: InventoryPal) {
  const stars = Math.min(Math.max(pal.rank, 0), 4)
  const works = pal.catalog.workSuitabilities
    .slice()
    .sort((left, right) => right.level - left.level
      || workSuitabilityOrder.indexOf(left.slug) - workSuitabilityOrder.indexOf(right.slug))
  const bonusById = new Map<number, number>()
  if (!works.length || !stars) return bonusById
  const currentById = new Map(works.map(work => [work.id, (pal.baseWorkSuitability[workSnapshotKey(pal, work)] ?? work.level) + (pal.workSuitabilityAddRanks[workSnapshotKey(pal, work)] ?? 0)]))
  const addPoint = (work: PalworldWorkSuitabilitySummary) => {
    const current = currentById.get(work.id) ?? work.level
    if (current >= 10) return
    currentById.set(work.id, current + 1)
    bonusById.set(work.id, (bonusById.get(work.id) ?? 0) + 1)
  }
  for (let star = 0; star < Math.min(stars, 3); star += 1) addPoint(works[star % works.length])
  if (stars >= 4) works.forEach(work => addPoint(work))
  return bonusById
}
function workCondensationBonus(pal: InventoryPal, work: PalworldWorkSuitabilitySummary) {
  return workCondensationBonuses(pal).get(work.id) ?? 0
}
function workExceedsBaseLevel(pal: InventoryPal, work: PalworldWorkSuitabilitySummary) {
  return workLevel(pal, work) > work.level
}
function passiveName(id: string) { return passiveById.value.get(id)?.name ?? id }
function passiveRankIconUrl(id: string) { return passiveById.value.get(id)?.rankIconUrl ?? undefined }
function clearFilters() {
  searchQuery.value = ''
  selectedElementIds.value = new Set(); selectedWorkSuitabilityIds.value = new Set(); selectedPassiveIds.value = new Set()
  selectedFavoriteLevels.value = new Set(); selectedLocationKeys.value = new Set(locationOptions.value.map(option => option.key)); requireAllPassives.value = false
  selectedRanks.value = new Set(); selectedIvSortKeys.value = new Set(); ivSortDirection.value = 'desc'; ivSortOnAverage.value = false
}
</script>

<template>
  <div class="inventory">
    <div v-if="serverDataStore.error || paldexStore.error" class="error-banner"><i class="mdi mdi-alert-circle-outline" />{{ serverDataStore.error ?? paldexStore.error }}</div>
    <div v-else-if="serverDataStore.loading || paldexStore.loading" class="status"><span class="spinner" />Chargement de l’inventaire…</div>
    <template v-else>
      <header class="inventory-header">
        <div class="inventory-toolbar">
          <input v-model="searchQuery" type="search" placeholder="Rechercher un Pal…" class="search-input">
          <div class="sort-controls">
            <select v-model="sortKey" class="toolbar-select">
              <option value="name">Nom</option><option value="paldex">Paldex</option><option value="level">Niveau du Pal</option><option value="hp">PV actuels</option><option value="workSpeed">Niveau d’aptitude</option>
            </select>
            <button type="button" class="toolbar-btn" :title="sortDirection === 'asc' ? 'Croissant' : 'Décroissant'" @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'"><i class="mdi" :class="sortDirection === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" /></button>
            <button type="button" class="toolbar-btn" title="Filtrer les emplacements" @click="openLocationSelector"><i class="mdi mdi-filter-variant" /></button>
          </div>
          <span class="pal-count"><strong>{{ visiblePals.length }}</strong> / {{ inventoryPals.length }} Pals</span>
        </div>
        <nav class="element-tabs">
          <button v-for="element in availableElements" :key="element.id" type="button" class="element-tab" :class="{ active: selectedElementIds.has(element.id) }" @click="toggleElement(element.id)"><img v-if="element.iconUrl" :src="element.iconUrl" alt="">{{ element.name }}</button>
        </nav>
        <nav class="element-tabs">
          <button v-for="work in availableWorkSuitabilities" :key="work.id" type="button" class="element-tab" :class="{ active: selectedWorkSuitabilityIds.has(work.id) }" @click="toggleWorkSuitability(work.id)"><img v-if="work.iconUrl" :src="work.iconUrl" alt="">{{ work.name }}</button>
        </nav>
        <div class="favorite-row">
          <button v-for="level in [1, 2, 3]" :key="`favorite-${level}`" type="button" class="favorite-lock-filter" :class="{ active: selectedFavoriteLevels.has(level) }" :title="`Favori ${level}`" @click="toggleFavorite(level)"><span class="favorite-lock-shape"><b>{{ level }}</b></span></button>
        </div>
        <div class="rank-filter-row" aria-label="Filtrer par étoiles de condensation">
          <button v-for="rank in [1, 2, 3, 4, 5]" :key="`rank-${rank}`" type="button" class="rank-filter" :class="{ active: selectedRanks.has(rank) }" :title="`${rank} étoile${rank > 1 ? 's' : ''}`" @click="toggleRank(rank)"><i v-for="star in rank" :key="star" class="mdi mdi-star" /></button>
        </div>
        <div class="iv-sort-row" aria-label="Trier par IV">
          <span class="iv-sort-label">IV</span>
          <button type="button" class="iv-sort-option" :class="{ active: selectedIvSortKeys.has('hp') }" title="Trier par IV PV" @click="toggleIvSort('hp')"><i class="mdi mdi-heart" /> PV</button>
          <button type="button" class="iv-sort-option" :class="{ active: selectedIvSortKeys.has('attack') }" title="Trier par IV attaque" @click="toggleIvSort('attack')"><i class="mdi mdi-sword-cross" /> ATK</button>
          <button type="button" class="iv-sort-option" :class="{ active: selectedIvSortKeys.has('defense') }" title="Trier par IV défense" @click="toggleIvSort('defense')"><i class="mdi mdi-shield" /> DEF</button>
          <button v-if="selectedIvSortKeys.size" type="button" class="toolbar-btn iv-sort-direction" :title="ivSortDirection === 'asc' ? 'IV croissants' : 'IV décroissants'" @click="ivSortDirection = ivSortDirection === 'asc' ? 'desc' : 'asc'"><i class="mdi" :class="ivSortDirection === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" /></button>
          <label v-if="selectedIvSortKeys.size > 1" class="iv-sort-average" title="Trier sur la moyenne des IV cochées plutôt que sur la meilleure"><input v-model="ivSortOnAverage" type="checkbox">Moyenne</label>
        </div>
        <div class="passive-filter">
          <button type="button" class="element-tab" @click="passiveSelectorOpen = true"><i class="mdi mdi-tune-variant" />{{ selectedPassiveIds.size ? 'Modifier les passifs' : 'Filtrer par passifs' }}</button>
          <label class="passive-match-control" :class="{ disabled: selectedPassiveIds.size > 4 }"><input v-model="requireAllPassives" type="checkbox" :disabled="!selectedPassiveIds.size || selectedPassiveIds.size > 4">Tous les passifs sélectionnés</label>
          <button v-for="passiveId in selectedPassiveIds" :key="passiveId" type="button" class="passive-option" :class="`rank-${passiveById.get(passiveId)?.rank ?? 0}`" :title="`Retirer ${passiveName(passiveId)}`" @click="togglePassive(passiveId)"><span>{{ passiveName(passiveId) }}</span><img v-if="passiveRankIconUrl(passiveId)" :src="passiveRankIconUrl(passiveId)" alt=""><i v-else class="mdi mdi-chevron-double-up" /></button>
        </div>
        <div v-if="searchQuery || selectedElementIds.size || selectedWorkSuitabilityIds.size || selectedPassiveIds.size || selectedFavoriteLevels.size || selectedRanks.size || selectedIvSortKeys.size || selectedLocationKeys.size !== locationOptions.length" class="clear-filters-row"><button type="button" class="clear-btn" @click="clearFilters"><i class="mdi mdi-close" />Effacer les filtres</button></div>
      </header>

      <section v-if="visiblePals.length" class="inventory-list">
        <article v-for="pal in displayedPals" :key="pal.instanceId" class="inventory-card">
          <div class="pal-top-meta"><span v-if="pal.level !== null" class="pal-level">Niv. {{ pal.level }}</span><span v-if="pal.rank > 0" class="condensation-rank" :title="`Condensation ${pal.rank}`"><i v-for="star in pal.rank" :key="star" class="mdi mdi-star" /></span></div>
          <span v-if="pal.favoriteIndex !== null && pal.favoriteIndex > 0" class="favorite-lock" :title="`Favori ${favoriteLevel(pal.favoriteIndex)}`"><span class="favorite-lock-shape"><b>{{ favoriteLevel(pal.favoriteIndex) }}</b></span></span>
          <img v-if="pal.catalog.imageUrl" :src="pal.catalog.imageUrl" :alt="pal.catalog.name" width="56" height="56" loading="lazy">
          <div class="inventory-card__identity"><small class="pal-index">#{{ pal.catalog.paldexIndex ?? '—' }}</small><strong>{{ pal.catalog.name }}</strong><small><i class="mdi" :class="pal.gender === 'male' ? 'mdi-gender-male gender-male' : pal.gender === 'female' ? 'mdi-gender-female gender-female' : 'mdi-help'" />{{ pal.ownerName ?? 'Propriétaire inconnu' }}</small><span class="pal-types"><span v-for="element in pal.catalog.elements" :key="element.id" class="element-icon-crop" :title="element.name" :style="element.iconUrl ? { backgroundImage: `url(${element.iconUrl})` } : {}" /></span></div>
          <div class="inventory-card__passives"><span v-for="passiveId in pal.passiveSkillIds" :key="passiveId" class="passive-option" :class="`rank-${passiveById.get(passiveId)?.rank ?? 0}`"><span>{{ passiveName(passiveId) }}</span><img v-if="passiveRankIconUrl(passiveId)" :src="passiveRankIconUrl(passiveId)" alt=""><i v-else class="mdi mdi-chevron-double-up" /></span></div>
          <aside class="inventory-card__iv" aria-label="IV"><div class="iv-stat"><i class="mdi mdi-heart" /><span :class="ivClass(ivValue(pal.ivHp))">{{ ivValue(pal.ivHp) }}</span></div><div class="iv-stat"><i class="mdi mdi-sword-cross" /><span :class="ivClass(ivValue(pal.ivAttack))">{{ ivValue(pal.ivAttack) }}</span></div><div class="iv-stat"><i class="mdi mdi-shield" /><span :class="ivClass(ivValue(pal.ivDefense))">{{ ivValue(pal.ivDefense) }}</span></div></aside>
          <div class="inventory-card__combat" aria-label="Statistiques actuelles"><div class="combat-stat"><i class="mdi mdi-heart" /><strong>PV</strong><span>{{ pal.currentHp !== null ? Math.round(pal.currentHp) : '—' }}</span></div><div class="combat-stat"><i class="mdi mdi-sword-cross" /><strong>ATK</strong><span>—</span></div><div class="combat-stat"><i class="mdi mdi-shield" /><strong>DEF</strong><span>—</span></div></div>
          <div v-if="pal.catalog.workSuitabilities.length" class="inventory-card__work"><span v-for="work in pal.catalog.workSuitabilities" :key="work.id" class="worksuitability" :title="`${work.name} : ${workLevel(pal, work)}`"><img v-if="work.iconUrl" :src="work.iconUrl" :alt="work.name" width="16" height="16" loading="lazy"><span class="worksuitability-level" :class="{ 'work-level--over-max': workExceedsBaseLevel(pal, work) }">{{ workLevel(pal, work) }}</span></span></div>
          <footer class="inventory-card__location"><i class="mdi" :class="pal.storageLocation === 'base' ? 'mdi-home-variant' : pal.storageLocation === 'party' ? 'mdi-account-group' : 'mdi-archive'" /><span>{{ storageLabel(pal.storageLocation) }}</span></footer>
        </article>
      </section>
      <div v-if="visiblePals.length" ref="inventorySentinel" class="sentinel">
        <div v-if="hasMoreInventoryPals" class="spinner" />
      </div>
      <p v-else class="empty">Aucun Pal ne correspond à la recherche.</p>
      <BreedingPassiveSelectorModal :open="passiveSelectorOpen" :passive-skills="passiveSkillsStore.passiveSkills" :available-passive-ids="[...availablePassiveIds]" :model-value="[...selectedPassiveIds]" :max-selections="0" @apply="applyPassiveSelection" @close="passiveSelectorOpen = false" />
      <Teleport to="body"><div v-if="locationSelectorOpen" class="location-modal-backdrop" @mousedown.self="locationSelectorOpen = false"><section class="location-modal" role="dialog" aria-modal="true"><header><strong>Emplacements</strong><button type="button" @click="locationSelectorOpen = false"><i class="mdi mdi-close" /></button></header><div class="location-modal__list"><button v-for="option in locationOptions" :key="option.key" type="button" :class="{ selected: pendingLocationKeys.has(option.key) }" @click="togglePendingLocation(option.key)"><i class="mdi" :class="option.icon" /><span>{{ option.label }}</span><i class="mdi" :class="pendingLocationKeys.has(option.key) ? 'mdi-check-circle' : 'mdi-circle-outline'" /></button></div><footer><button type="button" @click="applyLocationSelection">Valider</button></footer></section></div></Teleport>
    </template>
  </div>
</template>

<style scoped lang="scss">
.inventory { padding: 2rem; max-width: 1300px; margin: 0 auto; width: 100%; box-sizing: border-box; display: flex; flex-direction: column; gap: 1.25rem; }
.inventory-header { display: flex; flex-direction: column; gap: .8rem; }
.inventory-toolbar, .sort-controls, .element-tabs, .passive-filter, .inventory-card, .inventory-card footer { display: flex; align-items: center; }
.inventory-toolbar, .element-tabs, .passive-filter { gap: .5rem; flex-wrap: wrap; }
.element-tabs { width: 100%; justify-content: flex-start; align-content: flex-start; }
.search-input { width: 220px; height: 2rem; margin: 0; font-size: .75rem; }
.sort-controls { gap: .25rem; }
.toolbar-select { width: auto; min-width: 130px; height: 2rem; margin: 0; padding: 0 2rem 0 .5rem; font-size: .75rem; }
.toolbar-btn { display: flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; padding: 0; margin: 0; border: 1px solid var(--pico-form-element-border-color); border-radius: var(--pico-border-radius); background: var(--pico-form-element-background-color); color: var(--pico-form-element-color); }
.toolbar-btn:hover, .element-tab:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
.pal-count { margin-left: auto; color: var(--pico-muted-color); font-size: .75rem; white-space: nowrap; }.pal-count strong { color: var(--pico-primary); }
.element-tab, .clear-btn { display: inline-flex; align-items: center; gap: .35rem; width: auto; padding: .35rem .8rem; margin: 0; border: 1px solid var(--pico-card-border-color); border-radius: 999px; background: transparent; color: var(--pico-muted-color); font-size: .78rem; }.element-tab img { width: 16px; height: 16px; object-fit: contain; }.element-tab.active { border-color: var(--pico-primary); color: var(--pico-primary); font-weight: 700; }
.filter-separator { width: 1px; min-height: 1.5rem; background: var(--pico-card-border-color); margin: 0 .25rem; }
.passive-match-mode { display: inline-flex; align-items: center; gap: .3rem; color: var(--pico-muted-color); font-size: .75rem; }.passive-match-mode input { width: auto; height: auto; margin: 0; }.passive-match-mode.disabled { opacity: var(--pico-form-element-disabled-opacity); }
.passive-option { --passive-rank-color: var(--pico-muted-border-color); display: inline-flex; align-items: center; gap: .3rem; min-height: 26px; width: auto; padding: .22rem .4rem; margin: 0; border: 1px solid var(--pico-muted-border-color); border-left: 3px solid var(--passive-rank-color); border-radius: 0; background: color-mix(in srgb, var(--passive-rank-color) 7%, var(--pico-card-background-color)); color: var(--pico-color); font-size: .66rem; font-weight: 600; }.passive-option:hover { background: color-mix(in srgb, var(--passive-rank-color) 18%, var(--pico-card-background-color)); }.passive-option img, .passive-option i { width: 16px; height: 16px; object-fit: contain; color: var(--passive-rank-color); }.rank-5, .rank-4 { --passive-rank-color: #42d9ff; }.rank-3, .rank-2 { --passive-rank-color: #f5df39; }.rank-1 { --passive-rank-color: #dceaf0; }
.clear-btn { border-radius: 6px; }.inventory-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(290px, 1fr)); gap: .75rem; }.inventory-card { position: relative; align-items: flex-start; gap: .55rem; flex-wrap: wrap; padding: 1.8rem .7rem 2rem; border: 1px solid var(--pico-card-border-color); border-radius: 8px; background: var(--pico-card-background-color); }.inventory-card:hover { border-color: var(--pico-primary); }.inventory-card > img { border-radius: 6px; object-fit: cover; }.pal-index { color: var(--pico-muted-color); font-size: .6rem; font-weight: 700; }.inventory-card__identity { display: flex; flex: 1; flex-direction: column; min-width: 0; padding-top: .15rem; }.inventory-card__identity strong { font-size: .84rem; }.inventory-card__identity small { color: var(--pico-muted-color); font-size: .68rem; }.inventory-card__identity i { margin-right: .2rem; }.gender-male { color: #4a90d9; }.gender-female { color: #d9598f; }.favorite-lock { position: absolute; top: .35rem; right: .55rem; display: inline-flex; align-items: center; color: #f5df39; font-size: 1.25rem; line-height: 1; }.favorite-lock b { position: absolute; left: 50%; top: 56%; transform: translate(-50%, -50%); color: var(--pico-card-background-color); font-size: .72rem; font-weight: 900; font-family: sans-serif; }.inventory-card__passives { display: flex; flex: 1 0 100%; flex-wrap: wrap; gap: .25rem; }.inventory-card__location { position: absolute; right: .7rem; bottom: .5rem; gap: .25rem; color: var(--pico-muted-color); font-size: .68rem; }.inventory-card__location span { color: var(--pico-muted-color); }.status, .empty { padding: 2rem 0; text-align: center; color: var(--pico-muted-color); }.status { display: flex; justify-content: center; gap: .75rem; }.spinner { width: 20px; height: 20px; border: 2px solid var(--pico-card-border-color); border-top-color: var(--pico-primary); border-radius: 50%; animation: spin .8s linear infinite; }@keyframes spin { to { transform: rotate(360deg); } }.error-banner { padding: .75rem 1rem; color: var(--pico-del-color); border: 1px solid var(--pico-form-element-invalid-border-color); border-radius: 8px; }
.clear-btn { border-radius: 6px; }.inventory-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(290px, 1fr)); gap: .75rem; }.inventory-card { position: relative; align-items: flex-start; gap: .55rem; flex-wrap: wrap; padding: 1.8rem .7rem 2rem; border: 1px solid var(--pico-card-border-color); border-radius: 8px; background: var(--pico-card-background-color); }.inventory-card:hover { border-color: var(--pico-primary); }.inventory-card > img { border-radius: 6px; object-fit: cover; }.pal-index { color: var(--pico-muted-color); font-size: .6rem; font-weight: 700; }.inventory-card__identity { display: flex; flex: 1; flex-direction: column; min-width: 0; padding-top: .15rem; }.inventory-card__identity strong { font-size: .84rem; }.inventory-card__identity small { color: var(--pico-muted-color); font-size: .68rem; }.inventory-card__identity i { margin-right: .2rem; }.pal-types { display: flex; gap: .2rem; margin-top: .2rem; }.element-icon-crop { display: inline-block; width: 16px; height: 16px; border-radius: 3px; background-repeat: no-repeat; background-position: left center; background-size: auto 100%; }.gender-male { color: #4a90d9; }.gender-female { color: #d9598f; }.favorite-lock { position: absolute; top: .35rem; right: .55rem; display: inline-flex; align-items: center; color: #f5df39; font-size: 1.25rem; line-height: 1; }.favorite-lock b { position: absolute; left: 50%; top: 56%; transform: translate(-50%, -50%); color: var(--pico-card-background-color); font-size: .72rem; font-weight: 900; font-family: sans-serif; }.inventory-card__passives { display: flex; flex: 1 0 100%; flex-wrap: wrap; gap: .25rem; }.inventory-card__location { position: absolute; right: .7rem; bottom: .5rem; gap: .25rem; color: var(--pico-muted-color); font-size: .68rem; }.inventory-card__location span { color: var(--pico-muted-color); }.status, .empty { padding: 2rem 0; text-align: center; color: var(--pico-muted-color); }.status { display: flex; justify-content: center; gap: .75rem; }.spinner { width: 20px; height: 20px; border: 2px solid var(--pico-card-border-color); border-top-color: var(--pico-primary); border-radius: 50%; animation: spin .8s linear infinite; }@keyframes spin { to { transform: rotate(360deg); } }.error-banner { padding: .75rem 1rem; color: var(--pico-del-color); border: 1px solid var(--pico-form-element-invalid-border-color); border-radius: 8px; }

.favorite-row { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; }
.rank-filter-row { display: flex; align-items: center; gap: .25rem; flex-wrap: wrap; }
.rank-filter { display: inline-flex; align-items: center; gap: .02rem; width: auto; min-width: 1.8rem; height: 1.8rem; padding: 0 .2rem; margin: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .82rem; }
.rank-filter.active { color: #f5df39; }
.iv-sort-row { display: flex; align-items: center; gap: .3rem; flex-wrap: wrap; }
.iv-sort-label { margin-right: .15rem; color: var(--pico-muted-color); font-size: .72rem; font-weight: 700; }
.iv-sort-option { display: inline-flex; align-items: center; gap: .2rem; width: auto; height: 1.8rem; padding: 0 .45rem; margin: 0; border: 1px solid var(--pico-card-border-color); border-radius: 5px; background: transparent; color: var(--pico-muted-color); font-size: .68rem; font-weight: 700; }
.iv-sort-option i { color: #78dce8; }
.iv-sort-option.active { border-color: var(--pico-primary); color: var(--pico-primary); }
.iv-sort-direction { width: 1.8rem; height: 1.8rem; }
.iv-sort-average { display: inline-flex; align-items: center; gap: .3rem; margin: 0 0 0 .4rem; color: var(--pico-muted-color); font-size: .68rem; font-weight: 700; cursor: pointer; }
.iv-sort-average input[type='checkbox'] { appearance: auto; inline-size: .9rem; block-size: .9rem; min-inline-size: .9rem; margin: 0; accent-color: var(--pico-primary); opacity: 1; }
.inventory-card__work { display: flex; flex: 1 0 100%; flex-wrap: wrap; justify-content: flex-start; gap: 3px; margin-top: .15rem; }
.worksuitability { display: inline-flex; align-items: center; gap: 3px; padding: 2px 4px; border-radius: 4px; background: color-mix(in srgb, var(--pico-color) 8%, transparent); }
.worksuitability img { width: 16px; height: 16px; border-radius: 0; object-fit: contain; }
.worksuitability-level { color: var(--pico-color); font-size: 10px; font-weight: 700; }
.work-level--over-max { color: #f0bd3d; }
.favorite-row > span:not(.filter-separator) { color: var(--pico-muted-color); font-size: .78rem; }
.favorite-lock-filter { position: relative; display: inline-flex; align-items: center; justify-content: center; width: 1.8rem; height: 1.8rem; padding: 0; margin: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: 1.35rem; }
.favorite-lock-filter b { position: absolute; top: 56%; left: 50%; transform: translate(-50%, -50%); color: var(--pico-background-color); font-size: .82rem; font-weight: 900; font-family: sans-serif; line-height: 1; }
.favorite-lock-filter.active { color: #f5df39; }
.favorite-lock { color: #f5df39; }
.favorite-lock-shape { position: relative; display: grid; place-items: center; width: 1.15rem; height: .9rem; margin-top: .35rem; border-radius: 2px; background: currentColor; }
.favorite-lock-shape::before { position: absolute; bottom: calc(100% - .18rem); left: .16rem; width: .83rem; height: .72rem; border: .18rem solid currentColor; border-bottom: 0; border-radius: .55rem .55rem 0 0; content: ''; }
.favorite-lock-shape b { position: static; transform: none; color: #111111; font-size: .88rem; font-weight: 950; line-height: 1; }
.favorite-lock-filter .favorite-lock-shape b { font-size: .94rem; }
.inventory-card__stats { display: flex; flex: 1 0 100%; flex-direction: column; gap: .15rem; color: var(--pico-muted-color); font-size: .65rem; }.inventory-card__stats b { color: var(--pico-color); }.condensation-rank { color: #f5df39; font-size: .75rem; }
.pal-level { position: absolute; top: .45rem; left: .65rem; color: var(--pico-muted-color); font-size: .68rem; font-weight: 700; }
.location-modal-backdrop { position: fixed; z-index: 1100; inset: 0; display: grid; place-items: center; padding: 1rem; background: rgb(0 0 0 / 58%); backdrop-filter: blur(3px); }
.location-modal { display: flex; flex-direction: column; width: min(440px, 100%); max-height: min(600px, calc(100vh - 2rem)); margin: 0; overflow: hidden; border: 1px solid var(--pico-muted-border-color); border-radius: 12px; background: var(--pico-card-background-color); box-shadow: 0 18px 50px rgb(0 0 0 / 42%); }
.location-modal header, .location-modal footer { display: flex; align-items: center; justify-content: space-between; padding: .8rem 1rem; border-bottom: 1px solid var(--pico-card-border-color); }.location-modal footer { justify-content: flex-end; border-top: 1px solid var(--pico-card-border-color); border-bottom: 0; }.location-modal header button, .location-modal footer button { width: auto; margin: 0; }.location-modal header button { padding: 0; border: 0; background: transparent; color: var(--pico-muted-color); }
.location-modal__list { display: flex; flex-direction: column; gap: .35rem; overflow-y: auto; padding: .75rem; }.location-modal__list button { display: grid; grid-template-columns: auto 1fr auto; align-items: center; gap: .55rem; width: 100%; min-height: 2.4rem; padding: .45rem .6rem; margin: 0; border: 1px solid var(--pico-muted-border-color); background: transparent; color: var(--pico-color); text-align: left; }.location-modal__list button.selected { border-color: var(--pico-primary); color: var(--pico-primary); }.location-modal__list button span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.passive-match-mode { display: inline-flex; align-items: center; gap: .35rem; margin: 0; color: var(--pico-muted-color); font-size: .75rem; cursor: pointer; }
.passive-match-mode input[type='checkbox'] { width: auto; height: auto; margin: 0; }
.passive-match-mode.disabled { opacity: var(--pico-form-element-disabled-opacity); cursor: not-allowed; }
.passive-match-control { display: inline-flex; align-items: center; gap: .45rem; margin: 0; color: var(--pico-muted-color); font-size: .78rem; cursor: pointer; }
.passive-match-control input[type='checkbox'] { appearance: auto; inline-size: 1rem; block-size: 1rem; min-inline-size: 1rem; margin: 0; accent-color: var(--pico-primary); opacity: 1; }
.passive-match-control.disabled { opacity: var(--pico-form-element-disabled-opacity); cursor: not-allowed; }
.clear-filters-row { display: flex; justify-content: flex-end; }
.clear-filters-row .clear-btn { border-color: var(--pico-form-element-invalid-border-color); color: var(--pico-del-color); }
.clear-filters-row .clear-btn:hover { border-color: var(--pico-del-color); }
.pal-top-meta { position: absolute; top: .4rem; left: .65rem; display: flex; align-items: center; gap: .45rem; }
.pal-top-meta .pal-level { position: static; }
.pal-top-meta .condensation-rank { display: inline-flex; gap: .05rem; color: #f5df39; font-size: .82rem; line-height: 1; }
.inventory-card__combat { display: flex; flex: 1 0 100%; align-items: center; gap: 1rem; padding-top: .1rem; }
.combat-stat { display: inline-flex; align-items: center; gap: .25rem; color: var(--pico-color); font-size: .72rem; font-weight: 700; line-height: 1.1; }
.combat-stat i { width: .85rem; color: #78dce8; text-align: center; }
.combat-stat small { margin-left: .12rem; color: var(--pico-muted-color); font-size: .6rem; font-weight: 600; }
.inventory-card__iv { position: absolute; top: 1.9rem; right: .55rem; display: flex; flex-direction: column; align-items: flex-end; gap: .22rem; }
.iv-stat { display: inline-flex; align-items: center; gap: .2rem; font-size: .72rem; font-weight: 800; line-height: 1; }
.iv-stat i { width: .85rem; color: #78dce8; text-align: center; }
.iv-normal { color: var(--pico-color); }
.iv-high { color: #55d879; font-weight: 800; }
.iv-perfect { color: #f0bd3d; font-weight: 900; }
.current-hp { margin-top: .15rem; color: var(--pico-muted-color); font-size: .58rem; white-space: nowrap; }
.sentinel { display: flex; justify-content: center; align-items: center; min-height: 60px; }
@media (max-width: 640px) { .inventory { padding: 1rem; }.pal-count { margin-left: 0; }.filter-separator { display: none; } }
</style>
