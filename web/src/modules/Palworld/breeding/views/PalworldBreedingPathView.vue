<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { usePaldexStore } from '../../paldex/paldex.store'
import { useBreedingStore } from '../breeding.store'
import { fetchBreedingRules } from '../fetch/breeding.fetch'
import { BreedingPathEngine } from '../services/BreedingPathEngine'
import BreedingPathCanvas from '../components/BreedingPathCanvas.vue'
import BreedingPathTreeNode from '../components/BreedingPathTreeNode.vue'
import type { PalworldElementSummary, PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingPathResult } from '../types/breeding.types'

const paldexStore = usePaldexStore()
const breedingStore = useBreedingStore()

const ownedIds = ref<Set<number>>(new Set())
const targetPickerOpen = ref(false)
const selectedPalsOpen = ref(false)
const searchQuery = ref('')
const targetSearchQuery = ref('')
const selectedElementIds = ref<Set<number>>(new Set())

const result = ref<BreedingPathResult | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const pathEngine = ref<BreedingPathEngine | null>(null)

const palById = computed(() => new Map(paldexStore.pals.map(pal => [pal.id, pal])))
const normalizedQuery = computed(() => searchQuery.value.trim().toLocaleLowerCase())
const normalizedTargetQuery = computed(() => targetSearchQuery.value.trim().toLocaleLowerCase())

const availableElements = computed<PalworldElementSummary[]>(() => {
  const elements = new Map<number, PalworldElementSummary>()
  for (const pal of paldexStore.pals) {
    for (const element of pal.elements) elements.set(element.id, element)
  }
  return [...elements.values()].sort((a, b) => a.name.localeCompare(b.name))
})

const visibleOwnedPals = computed(() => paldexStore.pals.filter(pal =>
  (!normalizedQuery.value || pal.name.toLocaleLowerCase().includes(normalizedQuery.value))
  && (!selectedElementIds.value.size || pal.elements.some(element => selectedElementIds.value.has(element.id))),
))

const visibleTargetPals = computed(() => paldexStore.pals.filter(pal =>
  !normalizedTargetQuery.value || pal.name.toLocaleLowerCase().includes(normalizedTargetQuery.value),
))

function resolvePal(id: number): PalworldPalListItem | null {
  return palById.value.get(id) ?? null
}

const ownedPals = computed(() => [...ownedIds.value]
  .map(resolvePal)
  .filter((pal): pal is PalworldPalListItem => pal !== null))

const selectedPreviewPals = computed(() => ownedPals.value.slice(0, 5))
const remainingSelectedPals = computed(() => Math.max(0, ownedPals.value.length - selectedPreviewPals.value.length))

function toggleElement(id: number) {
  const next = new Set(selectedElementIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  selectedElementIds.value = next
}

function toggleOwned(pal: PalworldPalListItem) {
  const next = new Set(ownedIds.value)
  if (next.has(pal.id)) next.delete(pal.id)
  else next.add(pal.id)
  ownedIds.value = next
}

function selectAll() {
  ownedIds.value = new Set(paldexStore.pals.map(pal => pal.id))
}

function clearOwned() {
  ownedIds.value = new Set()
}

function selectTarget(pal: PalworldPalListItem) {
  breedingStore.selectPal(pal)
  targetPickerOpen.value = false
}

function toggleTargetPicker() {
  targetPickerOpen.value = true
  targetSearchQuery.value = ''
}

function returnToManualSelection() {
  targetPickerOpen.value = false
}

function computePath() {
  if (!breedingStore.selectedPal || ownedIds.value.size === 0 || !pathEngine.value) {
    result.value = null
    error.value = null
    return
  }
  try {
    result.value = pathEngine.value.compute(breedingStore.selectedPal.id, ownedIds.value)
  } catch {
    result.value = null
    error.value = 'Impossible de calculer le chemin.'
  }
}

watch([() => breedingStore.selectedPalId, ownedIds, pathEngine], computePath)

onMounted(async () => {
  try {
    await paldexStore.ensureLoaded()
    const rules = await fetchBreedingRules()
    pathEngine.value = new BreedingPathEngine(paldexStore.pals, rules)
  } catch {
    error.value = 'Le catalogue d’élevage est incomplet. Redémarre l’API pour charger les données à jour.'
  }
})
</script>

<template>
  <div class="path-finder">
    <aside class="path-sidebar">
      <div class="path-sidebar-content">
        <div class="path-summary" aria-label="Configuration du chemin d'élevage">
          <button type="button" class="source-card" :class="{ populated: ownedPals.length, active: !targetPickerOpen }" @click="returnToManualSelection">
            <div class="source-avatars">
              <img
                v-for="pal in selectedPreviewPals"
                :key="pal.id"
                :src="pal.imageUrl ?? ''"
                :alt="pal.name"
              >
              <span v-if="remainingSelectedPals" class="more-pals">+{{ remainingSelectedPals }}</span>
              <i v-if="ownedPals.length === 0" class="mdi mdi-paw-outline source-empty-icon" />
            </div>
            <strong>{{ ownedPals.length }} Pal{{ ownedPals.length !== 1 ? 's' : '' }}</strong>
            <small>Sélection manuelle</small>
          </button>

          <i class="mdi mdi-arrow-right path-arrow" aria-hidden="true" />

          <button
            type="button"
            class="target-card"
            :class="{ active: targetPickerOpen, populated: breedingStore.selectedPal }"
            @click="toggleTargetPicker"
          >
            <template v-if="breedingStore.selectedPal">
              <img :src="breedingStore.selectedPal.imageUrl ?? ''" :alt="breedingStore.selectedPal.name">
              <strong>{{ breedingStore.selectedPal.name }}</strong>
              <small>Pal cible</small>
            </template>
            <template v-else>
              <i class="mdi mdi-plus" />
              <strong>Choisir une cible</strong>
              <small>Pal cible</small>
            </template>
          </button>
        </div>

        <button type="button" class="passive-button" disabled>
          <i class="mdi mdi-plus" /> Ajouter un passif
        </button>

        <template v-if="!targetPickerOpen">
          <div class="source-switch" role="group" aria-label="Source des Pals">
            <button type="button" class="active">Sélection manuelle</button>
            <button type="button" disabled title="Bientôt disponible">Pals du serveur</button>
          </div>

          <section class="selected-pals-panel">
            <div class="selected-pals-heading">
              <span><strong>Pals sélectionnés</strong> <small>{{ ownedPals.length }}</small></span>
              <span class="selected-actions">
                <button type="button" @click="selectAll">Tout sélectionner</button>
                <button type="button" @click="clearOwned">Effacer</button>
              </span>
            </div>

            <button type="button" class="selected-pals-toggle" @click="selectedPalsOpen = !selectedPalsOpen">
              <span class="selected-preview">
                <img v-for="pal in selectedPreviewPals" :key="pal.id" :src="pal.imageUrl ?? ''" :alt="pal.name">
                <span v-if="remainingSelectedPals">+{{ remainingSelectedPals }}</span>
                <small v-if="ownedPals.length === 0">Aucun Pal sélectionné</small>
              </span>
              <i class="mdi" :class="selectedPalsOpen ? 'mdi-chevron-up' : 'mdi-chevron-down'" />
            </button>

            <div v-if="selectedPalsOpen" class="selected-pals-list">
              <button v-for="pal in ownedPals" :key="pal.id" type="button" @click="toggleOwned(pal)">
                <img :src="pal.imageUrl ?? ''" :alt="pal.name">
                <span>{{ pal.name }}</span>
                <i class="mdi mdi-close" />
              </button>
            </div>
          </section>

          <div class="search-toolbar">
            <div class="pal-search">
              <i class="mdi mdi-magnify" />
              <input v-model="searchQuery" type="search" placeholder="Rechercher un Pal...">
            </div>
            <span class="pal-count"><strong>{{ visibleOwnedPals.length }}</strong> / {{ paldexStore.pals.length }} Pals</span>
          </div>

          <div class="element-filters" aria-label="Filtrer par type">
            <button
              v-for="element in availableElements"
              :key="element.id"
              type="button"
              :class="{ active: selectedElementIds.has(element.id) }"
              @click="toggleElement(element.id)"
            >
              <img v-if="element.iconUrl" :src="element.iconUrl" alt="" aria-hidden="true">
              {{ element.name }}
            </button>
          </div>

          <div class="owned-pal-list">
            <button
              v-for="pal in visibleOwnedPals"
              :key="pal.id"
              type="button"
              :class="{ selected: ownedIds.has(pal.id) }"
              @click="toggleOwned(pal)"
            >
              <img :src="pal.imageUrl ?? ''" :alt="pal.name" loading="lazy">
              <span>{{ pal.name }}</span>
              <span class="pal-elements">
                <img v-for="element in pal.elements" :key="element.id" :src="element.iconUrl ?? ''" :alt="element.name" :title="element.name">
              </span>
              <i class="mdi" :class="ownedIds.has(pal.id) ? 'mdi-check-circle' : 'mdi-plus-circle-outline'" />
            </button>
            <p v-if="visibleOwnedPals.length === 0">Aucun Pal ne correspond à la recherche.</p>
          </div>
        </template>

        <template v-else>
          <div class="target-picker-heading"><strong>Choisir le Pal cible</strong></div>
          <div class="search-toolbar">
            <div class="pal-search">
              <i class="mdi mdi-magnify" />
              <input v-model="targetSearchQuery" type="search" placeholder="Rechercher le Pal cible...">
            </div>
            <span class="pal-count"><strong>{{ visibleTargetPals.length }}</strong> / {{ paldexStore.pals.length }} Pals</span>
          </div>
          <div class="target-pal-list">
            <button v-for="pal in visibleTargetPals" :key="pal.id" type="button" @click="selectTarget(pal)">
              <img :src="pal.imageUrl ?? ''" :alt="pal.name" loading="lazy">
              <span>{{ pal.name }}</span>
            </button>
          </div>
        </template>
      </div>
    </aside>

    <section class="path-result" aria-live="polite">
      <p v-if="error" class="error-banner"><i class="mdi mdi-alert-circle-outline" /> {{ error }}</p>
      <div v-else-if="loading" class="status"><span class="spinner" /> Calcul du chemin…</div>
      <template v-else-if="result">
        <div v-if="!result.reachable || !result.root" class="empty">
          <i class="mdi mdi-map-marker-path" />
          <strong>Aucun chemin d'élevage trouvé</strong>
          <span>Les Pals sélectionnés ne permettent pas d'obtenir {{ breedingStore.selectedPal?.name }}.</span>
        </div>
        <BreedingPathCanvas v-else class="path-tree-wrap" :reset-key="result.root.species.id">
          <BreedingPathTreeNode :node="result.root" :resolve-pal="resolvePal" is-root />
        </BreedingPathCanvas>
      </template>
      <div v-else class="empty-state">
        <i class="mdi mdi-source-branch" />
        <strong>Choisis un Pal cible</strong>
        <span>Le chemin d'élevage apparaîtra ici.</span>
      </div>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.path-finder { display: grid; grid-template-columns: 340px minmax(0, 1fr); gap: 2rem; align-items: stretch; height: calc(100dvh - 10.5rem); overflow: hidden; }
.path-sidebar { min-width: 0; min-height: 0; }
.path-sidebar-content { position: fixed; top: calc(var(--header-height, 56px) + 124px); left: max(2rem, calc((100dvw - 1800px) / 2 + 2rem)); display: flex; flex-direction: column; gap: .7rem; width: 340px; height: calc(100dvh - var(--header-height, 56px) - 132px); min-height: 0; padding: .2rem; overflow: hidden; }
.path-summary { display: grid; grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr); gap: .55rem; align-items: center; }
.source-card, .target-card { box-sizing: border-box; min-width: 0; min-height: 96px; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: .25rem; padding: .65rem .5rem; border: 1px dashed var(--pico-card-border-color); border-radius: var(--pico-border-radius); background: var(--pico-card-background-color); color: var(--pico-muted-color); }
.source-card.populated { border-style: solid; border-color: var(--pico-primary); color: var(--pico-color); }
.target-card.populated { border-style: solid; color: var(--pico-color); }
.source-card, .target-card { margin: 0; cursor: pointer; font: inherit; }
.source-card:hover, .target-card:hover { border-color: var(--pico-primary); color: var(--pico-color); }
.source-card.active, .target-card.active { border: 2px solid var(--pico-primary); box-shadow: inset 0 0 0 1px var(--pico-primary); }
.target-card > .mdi { font-size: 1.35rem; color: var(--pico-primary); }
.target-card > img { width: 42px; height: 42px; object-fit: contain; }
.source-card strong, .target-card strong { max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: .72rem; }
.source-card small, .target-card small { color: var(--pico-primary); font-size: .56rem; letter-spacing: .06em; text-transform: uppercase; }
.source-avatars { display: flex; align-items: center; justify-content: center; min-height: 42px; }
.source-avatars img, .selected-preview img { width: 31px; height: 31px; object-fit: contain; border-radius: 50%; background: #fff; border: 1px solid var(--pico-primary); margin-left: -7px; }
.source-avatars img:first-child, .selected-preview img:first-child { margin-left: 0; }
.more-pals, .selected-preview > span { display: grid; place-items: center; width: 29px; height: 29px; margin-left: -7px; border-radius: 50%; background: var(--pico-secondary-background); color: var(--pico-secondary-inverse); font-size: .58rem; }
.source-empty-icon { font-size: 1.5rem; }
.path-arrow { font-size: 1.15rem; color: var(--pico-primary); }
.passive-button { margin: 0; padding: .35rem; border: 1px dashed var(--pico-card-border-color); background: transparent; color: var(--pico-muted-color); font-size: .68rem; }
.source-switch { display: grid; grid-template-columns: 1fr 1fr; padding: .2rem; border: 1px solid var(--pico-card-border-color); border-radius: var(--pico-border-radius); }
.source-switch button { margin: 0; padding: .45rem .25rem; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .68rem; }
.source-switch button.active { background: var(--pico-primary-background); color: var(--pico-primary-inverse); border-radius: calc(var(--pico-border-radius) - 2px); }
.selected-pals-panel { display: flex; flex-direction: column; gap: .35rem; }
.selected-pals-heading { display: flex; justify-content: space-between; align-items: center; font-size: .67rem; }
.selected-pals-heading small { color: var(--pico-muted-color); }
.selected-actions { display: flex; gap: .55rem; }
.selected-actions button { margin: 0; padding: 0; border: 0; background: transparent; color: var(--pico-muted-color); font-size: .58rem; }
.selected-actions button:hover { color: var(--pico-primary); }
.selected-pals-toggle { display: flex; align-items: center; justify-content: space-between; min-height: 45px; margin: 0; padding: .35rem .5rem; border: 1px solid var(--pico-card-border-color); background: var(--pico-card-background-color); color: var(--pico-color); }
.selected-preview { display: flex; align-items: center; min-width: 0; }
.selected-preview small { color: var(--pico-muted-color); font-size: .65rem; }
.selected-pals-list { display: grid; grid-template-columns: 1fr 1fr; gap: .3rem; max-height: 105px; overflow: auto; }
.selected-pals-list button { display: flex; align-items: center; min-width: 0; gap: .25rem; margin: 0; padding: .25rem; border: 1px solid var(--pico-card-border-color); background: var(--pico-card-background-color); color: var(--pico-color); font-size: .6rem; }
.selected-pals-list img { width: 21px; height: 21px; object-fit: contain; }
.selected-pals-list span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.selected-pals-list .mdi { margin-left: auto; color: var(--pico-muted-color); }
.search-toolbar { display: flex; align-items: center; gap: .45rem; }
.pal-search { display: flex; flex: 1; align-items: center; gap: .35rem; padding: 0 .55rem; border: 1px solid var(--pico-card-border-color); border-radius: var(--pico-border-radius); background: var(--pico-card-background-color); }
.pal-search > .mdi { color: var(--pico-muted-color); }
.pal-search input { min-width: 0; height: 2rem; margin: 0; padding: 0; border: 0; background: transparent; box-shadow: none; font-size: .72rem; }
.pal-search input:focus { box-shadow: none; }
.pal-count { margin-left: auto; color: var(--pico-muted-color); font-size: .58rem; white-space: nowrap; }
.pal-count strong { color: var(--pico-primary); }
.element-filters { display: flex; flex-wrap: wrap; gap: .28rem; }
.element-filters button { display: inline-flex; align-items: center; gap: .18rem; margin: 0; padding: .25rem .37rem; border: 1px solid transparent; background: transparent; color: var(--pico-muted-color); font-size: .62rem; }
.element-filters button.active { border-color: var(--pico-primary); color: var(--pico-primary); }
.element-filters img { width: 14px; height: 14px; object-fit: contain; }
.owned-pal-list, .target-pal-list { min-height: 0; overflow: auto; }
.owned-pal-list { display: flex; flex: 1; flex-direction: column; gap: .35rem; padding-right: .15rem; overscroll-behavior: contain; }
.owned-pal-list > button { display: grid; grid-template-columns: 34px minmax(0, 1fr) auto 18px; gap: .4rem; align-items: center; min-height: 46px; margin: 0; padding: .3rem .4rem; border: 1px solid var(--pico-card-border-color); background: var(--pico-card-background-color); color: var(--pico-color); text-align: left; font-size: .68rem; }
.owned-pal-list > button.selected { border-color: var(--pico-primary); background: color-mix(in srgb, var(--pico-primary-background) 18%, var(--pico-card-background-color)); }
.owned-pal-list > button > img { width: 34px; height: 34px; object-fit: contain; }
.owned-pal-list > button > span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.owned-pal-list > button > .mdi { color: var(--pico-primary); font-size: 1rem; }
.pal-elements { display: flex; gap: .1rem; }
.pal-elements img { width: 15px; height: 15px; object-fit: contain; }
.owned-pal-list > p { margin: 1rem 0; color: var(--pico-muted-color); text-align: center; font-size: .7rem; }
.target-picker-heading { display: flex; align-items: center; font-size: .76rem; }
.target-pal-list { display: grid; grid-template-columns: 1fr 1fr; align-content: start; gap: .35rem; flex: 1; padding-right: .15rem; }
.target-pal-list button { display: flex; align-items: center; min-width: 0; gap: .35rem; min-height: 43px; margin: 0; padding: .28rem; border: 1px solid var(--pico-card-border-color); background: var(--pico-card-background-color); color: var(--pico-color); text-align: left; font-size: .65rem; }
.target-pal-list img { width: 33px; height: 33px; object-fit: contain; }
.target-pal-list span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.path-result { display: flex; align-items: center; justify-content: center; min-width: 0; min-height: 0; }
.path-result :deep(.path-node-card.owned .breeding-chip-avatar) { border-radius: 50%; background: #fff; }
.path-result :deep(.path-node-card .breeding-chip-avatar img) { image-rendering: auto; }
.path-tree-wrap { width: 100%; }
.empty-state, .status, .empty { display: flex; flex-direction: column; align-items: center; gap: .5rem; color: var(--pico-muted-color); text-align: center; }
.empty-state .mdi, .empty .mdi { font-size: 3rem; color: var(--pico-primary); }
.empty-state strong, .empty strong { color: var(--pico-color); font-size: 1.05rem; }
.empty-state span, .empty span { font-size: .8rem; }
.spinner { width: 20px; height: 20px; border: 2px solid var(--pico-card-border-color); border-top-color: var(--pico-primary); border-radius: 50%; animation: spin .8s linear infinite; }
.error-banner { margin: 0; color: var(--pico-del-color); }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 1050px) { .path-finder { grid-template-columns: 1fr; height: auto; overflow: visible; } .path-sidebar-content { position: static; width: auto; height: auto; overflow: visible; } .owned-pal-list, .target-pal-list { max-height: 480px; } .path-result { min-height: 360px; } }
@media (max-width: 480px) { .path-summary { grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr); } .target-pal-list { grid-template-columns: 1fr; } }
</style>
