<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { usePaldexStore } from '../../paldex/paldex.store'
import { useBreedingStore } from '../breeding.store'
import { fetchBreedingResult } from '../fetch/breeding.fetch'
import BreedingSquare from '../components/BreedingSquare.vue'
import BreedingPalPicker from '../components/BreedingPalPicker.vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingResult } from '../types/breeding.types'

const route = useRoute()
const router = useRouter()
const paldexStore = usePaldexStore()
const breedingStore = useBreedingStore()

type Slot = 'parentA' | 'parentB'

// Parent A vit dans breedingStore (partagé avec les autres vues du module et reflété dans l'URL).
// Parent B reste local : c'est un choix propre au calculateur, jamais partagé ni présent dans l'URL.
const parentB = ref<PalworldPalListItem | null>(null)
const openPicker = ref<Slot | null>(null)
const result = ref<BreedingResult | null>(null)
const loadingResult = ref(false)
const resultError = ref<string | null>(null)

function selectPal(slot: Slot, pal: PalworldPalListItem) {
  if (slot === 'parentA') breedingStore.selectPal(pal)
  else parentB.value = pal

  openPicker.value = slot === 'parentA' && !parentB.value ? 'parentB' : null
}

function clearAll() {
  breedingStore.selectPal(null)
  parentB.value = null
  result.value = null
  resultError.value = null
  openPicker.value = 'parentA'
}

async function computeResult() {
  if (!breedingStore.selectedPal || !parentB.value) return

  loadingResult.value = true
  resultError.value = null
  try {
    result.value = await fetchBreedingResult(breedingStore.selectedPal.id, parentB.value.id)
  } catch {
    result.value = null
    resultError.value = "Impossible de calculer le résultat de l'élevage."
  } finally {
    loadingResult.value = false
  }
}

const resultPal = computed<PalworldPalListItem | null>(() => {
  if (!result.value) return null
  return paldexStore.pals.find(p => p.id === result.value!.child.id) ?? null
})

watch([() => breedingStore.selectedPal, parentB], () => {
  result.value = null
  resultError.value = null
  if (breedingStore.selectedPal && parentB.value) computeResult()
})

// URL -> store : source de vérité au chargement, et réactif ensuite (pas juste onMounted) pour que
// "Voir l'élevage de {enfant}" (même route, query différente) fonctionne aussi sans rechargement.
function resolvePalFromQuery(rawId: unknown) {
  if (!rawId) return
  const palId = Number(Array.isArray(rawId) ? rawId[0] : rawId)
  if (!Number.isFinite(palId) || breedingStore.selectedPalId === palId) return

  const pal = paldexStore.pals.find(p => p.id === palId)
  if (pal) {
    breedingStore.selectPal(pal)
    parentB.value = null
    openPicker.value = 'parentB'
  }
}

watch(() => route.query.pal, resolvePalFromQuery)

// Store -> URL : reflète en direct le Parent A choisi (pas seulement sa valeur au chargement), pour
// que le `?pal=` reste toujours exact quand on partage le lien ou qu'on change d'onglet (BreedingNav
// copie route.query en changeant de vue — sans ça la vue Recherche hériterait d'un pal figé/périmé).
watch(() => breedingStore.selectedPalId, (palId) => {
  const current = route.query.pal
  const currentId = current ? Number(Array.isArray(current) ? current[0] : current) : null
  if (palId === currentId) return

  router.replace({ query: { ...route.query, pal: palId ? String(palId) : undefined } })
})

onMounted(async () => {
  await paldexStore.ensureLoaded()
  resolvePalFromQuery(route.query.pal)
  if (!breedingStore.selectedPal) openPicker.value = 'parentA'
})

function goToBreedingOf(pal: PalworldPalListItem) {
  breedingStore.selectPal(pal)
  parentB.value = null
  openPicker.value = 'parentB'
}
</script>

<template>
  <div class="breeding-calculator">
    <div class="breeding-squares">
      <BreedingSquare
        label="Parent A"
        :pal="breedingStore.selectedPal"
        clickable
        :active="openPicker === 'parentA'"
        @click="openPicker = 'parentA'"
      />

      <span class="breeding-operator">+</span>

      <BreedingSquare
        label="Parent B"
        :pal="parentB"
        clickable
        :active="openPicker === 'parentB'"
        @click="openPicker = 'parentB'"
      />

      <span class="breeding-operator">=</span>

      <BreedingSquare
        label="Résultat"
        :pal="resultPal"
        :loading="loadingResult"
      />
    </div>

    <p v-if="resultError" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ resultError }}
    </p>

    <p v-else-if="result" class="result-detail">
      <template v-if="result.rule === 'exception'">
        Combinaison spéciale (exception de reproduction).
      </template>
      <template v-else-if="result.formula">
        Formule : rang cible {{ result.formula.targetRank }}, distance {{ result.formula.distance }}.
      </template>
      <button type="button" class="link-btn" :disabled="!resultPal" @click="resultPal && goToBreedingOf(resultPal)">
        Voir l'élevage de {{ result.child.name }}
      </button>
    </p>

    <div v-if="breedingStore.selectedPal || parentB" class="breeding-reset">
      <button type="button" class="clear-btn" @click="clearAll">
        Recommencer
      </button>
    </div>

    <div v-if="openPicker" class="breeding-picker-panel">
      <BreedingPalPicker @select="pal => selectPal(openPicker!, pal)" />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.breeding-calculator {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding-bottom: 2rem;
}

.breeding-squares {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1.25rem;
  flex-wrap: wrap;
}

.breeding-operator {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--pico-muted-color);
}

.result-detail {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  text-align: center;
  color: var(--pico-muted-color);
  font-size: 0.85rem;
}

.link-btn {
  width: auto;
  margin: 0;
  padding: 0.35rem 0.9rem;
  font-size: 0.8rem;
  border-radius: 999px;
  border: 1px solid var(--pico-primary);
  background: transparent;
  color: var(--pico-primary);
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
  }
}

.breeding-reset {
  display: flex;
  justify-content: center;
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

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-contrast);
  }
}

.error-banner {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
  margin: 0;
}

.breeding-picker-panel {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--pico-card-border-color);
}
</style>
