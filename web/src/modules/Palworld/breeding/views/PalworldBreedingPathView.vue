<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { usePaldexStore } from '../../paldex/paldex.store'
import { useBreedingStore } from '../breeding.store'
import { fetchBreedingPath } from '../fetch/breeding.fetch'
import BreedingPalPicker from '../components/BreedingPalPicker.vue'
import BreedingPalListRow from '../components/BreedingPalListRow.vue'
import BreedingPathTreeNode from '../components/BreedingPathTreeNode.vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingPathResult } from '../types/breeding.types'

const paldexStore = usePaldexStore()
const breedingStore = useBreedingStore()

const ownedIds = ref<Set<number>>(new Set())
const targetPickerOpen = ref(false)
const ownedPickerOpen = ref(false)

const result = ref<BreedingPathResult | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

const palById = computed(() => new Map(paldexStore.pals.map(pal => [pal.id, pal])))

function resolvePal(id: number): PalworldPalListItem | null {
  return palById.value.get(id) ?? null
}

const ownedPals = computed(() => [...ownedIds.value].map(resolvePal).filter((p): p is PalworldPalListItem => p !== null))

function selectTarget(pal: PalworldPalListItem) {
  breedingStore.selectPal(pal)
  targetPickerOpen.value = false
}

function toggleOwned(pal: PalworldPalListItem) {
  const next = new Set(ownedIds.value)
  if (next.has(pal.id)) next.delete(pal.id)
  else next.add(pal.id)
  ownedIds.value = next
}

function removeOwned(palId: number) {
  const next = new Set(ownedIds.value)
  next.delete(palId)
  ownedIds.value = next
}

async function computePath() {
  if (!breedingStore.selectedPal || ownedIds.value.size === 0) {
    result.value = null
    return
  }

  loading.value = true
  error.value = null
  try {
    result.value = await fetchBreedingPath(breedingStore.selectedPal.id, [...ownedIds.value])
  } catch {
    result.value = null
    error.value = 'Impossible de calculer le chemin.'
  } finally {
    loading.value = false
  }
}

// Recalcule automatiquement dès que la cible ou la liste "Mes Pals" change (suppression incluse) —
// pas besoin de recliquer sur un bouton.
watch([() => breedingStore.selectedPalId, ownedIds], computePath)

onMounted(async () => {
  await paldexStore.ensureLoaded()
})
</script>

<template>
  <div class="path-finder">
    <aside class="path-sidebar">
      <div class="setup-block">
        <span class="setup-label">Pal cible</span>
        <BreedingPalListRow
          v-if="breedingStore.selectedPal"
          :pal="breedingStore.selectedPal"
          clickable
          @click="targetPickerOpen = !targetPickerOpen"
        />
        <button v-else type="button" class="empty-slot" @click="targetPickerOpen = !targetPickerOpen">
          <i class="mdi mdi-plus" /> Choisir un Pal
        </button>
      </div>

      <div v-if="targetPickerOpen" class="picker-panel">
        <BreedingPalPicker @select="selectTarget" />
      </div>

      <div class="setup-block">
        <span class="setup-label">Mes Pals ({{ ownedPals.length }})</span>
        <div class="owned-rows">
          <BreedingPalListRow
            v-for="pal in ownedPals"
            :key="pal.id"
            :pal="pal"
            removable
            @remove="removeOwned(pal.id)"
          />
        </div>
        <button type="button" class="add-owned-btn" @click="ownedPickerOpen = !ownedPickerOpen">
          <i class="mdi mdi-plus" /> Ajouter un Pal
        </button>
      </div>

      <div v-if="ownedPickerOpen" class="picker-panel">
        <BreedingPalPicker mode="multiple" :selected-ids="ownedIds" @toggle="toggleOwned" />
      </div>
    </aside>

    <section class="path-result">
      <p v-if="error" class="error-banner">
        <i class="mdi mdi-alert-circle-outline" />
        {{ error }}
      </p>

      <div v-else-if="loading" class="status">
        <span class="spinner" />
        Calcul du chemin…
      </div>

      <template v-else-if="result">
        <p v-if="!result.reachable" class="empty">
          Aucun chemin trouvé vers {{ breedingStore.selectedPal?.name }} avec les Pals sélectionnés.
        </p>

        <div v-else-if="result.root" class="path-tree-wrap">
          <BreedingPathTreeNode :node="result.root" :resolve-pal="resolvePal" is-root />
        </div>
      </template>

      <p v-else class="empty-state">
        Choisis un Pal cible et sélectionne les Pals que tu possèdes à gauche.
      </p>
    </section>
  </div>
</template>

<style lang="scss" scoped>
.path-finder {
  display: flex;
  gap: 2rem;
  align-items: flex-start;
  padding-bottom: 2rem;
}

@media (max-width: 900px) {
  .path-finder {
    flex-direction: column;
  }
}

.path-sidebar {
  width: 300px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  position: sticky;
  top: 1rem;
}

@media (max-width: 900px) {
  .path-sidebar {
    width: 100%;
    position: static;
  }
}

.setup-block {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.setup-label {
  font-size: 0.72rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
}

.empty-slot {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  margin: 0;
  padding: 0.4rem 0.75rem;
  font-size: 0.8rem;
  border-radius: 6px;
  border: 1px dashed var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
}

.owned-rows {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  max-height: 40vh;
  overflow-y: auto;
}

.add-owned-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.3rem;
  margin: 0;
  padding: 0.4rem 0.75rem;
  font-size: 0.78rem;
  border-radius: 6px;
  border: 1px dashed var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
}

.picker-panel {
  padding: 0.75rem;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
}

.path-result {
  flex: 1;
  min-width: 0;
  min-height: 300px;
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
  margin: 0;
}

.empty, .empty-state {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 3rem 0;
}

.path-tree-wrap {
  overflow-x: auto;
  padding: 2rem 1rem;
  display: flex;
  justify-content: center;
}
</style>
