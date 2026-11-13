<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import { useHdvSnifferStore, type SnifferBatchCapture } from '../hdvSniffer.store'

//* Une liste de prix moyens reçue d'un bloc (plusieurs milliers d'objets). Le détail n'est
//* jamais rendu en entier : une page de PAGE_SIZE lignes à la fois.
const PAGE_SIZE = 50

const props = defineProps<{
  capture: SnifferBatchCapture
  isExpanded: boolean
}>()

const emit = defineEmits(['toggle'])

const hdvStore = useHdvSnifferStore()
const search = ref('')
const page = ref(0)

const nameOf = (assetId: number) =>
  hdvStore.metadataByAsset(props.capture.versionId, assetId)?.name ?? null

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return props.capture.entries
  return props.capture.entries.filter(e =>
    String(e.id).includes(q) || (nameOf(e.id)?.toLowerCase().includes(q) ?? false)
  )
})

const pageCount = computed(() => Math.max(1, Math.ceil(filtered.value.length / PAGE_SIZE)))
const pageEntries = computed(() => filtered.value.slice(page.value * PAGE_SIZE, (page.value + 1) * PAGE_SIZE))

watch(search, () => (page.value = 0))

const time = computed(() =>
  new Date(props.capture.timestamp).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
)
</script>

<template>
  <div class="batch-group" :class="{ 'is-expanded': isExpanded }">
    <div class="batch-summary" @click="emit('toggle')">
      <span class="mdi mdi-format-list-numbered batch-icon" />
      <div class="batch-text">
        <span class="batch-title">Prix moyens Dofus 3</span>
        <span class="batch-meta">
          {{ formatNumber(capture.savedCount) }} / {{ formatNumber(capture.pricedCount) }} enregistrés
          · {{ formatNumber(capture.count) }} objets reçus à {{ time }}
          <template v-if="capture.unknownCount > 0">
            · {{ formatNumber(capture.unknownCount) }} absents de la base
          </template>
        </span>
      </div>
      <span class="mdi chevron" :class="isExpanded ? 'mdi-chevron-up' : 'mdi-chevron-down'" />
    </div>

    <div v-if="isExpanded" class="batch-details">
      <input v-model="search" type="search" class="batch-search" placeholder="Filtrer par nom ou id" />

      <div class="batch-table">
        <div v-for="entry in pageEntries" :key="entry.id" class="batch-line">
          <span class="name" :class="{ unknown: !nameOf(entry.id) }">{{ nameOf(entry.id) ?? `#${entry.id}` }}</span>
          <span class="price">{{ formatNumber(entry.price) }} <small>k</small></span>
        </div>
        <p v-if="pageEntries.length === 0" class="empty">Aucun objet</p>
      </div>

      <div class="pager">
        <button class="outline secondary" :disabled="page === 0" @click="page--">
          <span class="mdi mdi-chevron-left" />
        </button>
        <span>Page {{ page + 1 }} / {{ pageCount }} · {{ formatNumber(filtered.length) }} objets</span>
        <button class="outline secondary" :disabled="page >= pageCount - 1" @click="page++">
          <span class="mdi mdi-chevron-right" />
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.batch-group {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.5rem;
  overflow: hidden;

  &.is-expanded { border-color: var(--pico-primary-border); box-shadow: var(--pico-card-box-shadow); }
}

.batch-summary {
  display: flex; align-items: center; gap: 1rem; padding: 0.75rem 1rem;
  cursor: pointer; user-select: none;
  &:hover { background: var(--pico-card-sectioning-background-color); }
}

.batch-icon { font-size: 1.4rem; color: var(--pico-primary); }
.batch-text { display: flex; flex-direction: column; flex: 1; min-width: 0; }
.batch-title { font-weight: 600; }
.batch-meta { font-size: 0.75rem; color: var(--pico-muted-color); }
.chevron { color: var(--pico-muted-color); }

.batch-details {
  border-top: 1px solid var(--pico-muted-border-color);
  padding: 0.75rem 1rem;
  display: flex; flex-direction: column; gap: 0.5rem;
}

.batch-search { margin: 0; height: 2rem; padding: 0 0.6rem; font-size: 0.8rem; }

.batch-table { display: flex; flex-direction: column; }

.batch-line {
  display: flex; justify-content: space-between; gap: 1rem;
  padding: 0.25rem 0.4rem; font-size: 0.8rem;
  &:nth-child(odd) { background: var(--pico-card-sectioning-background-color); }
  .name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; &.unknown { color: var(--pico-muted-color); } }
  .price { font-weight: 600; white-space: nowrap; }
}

.empty { margin: 0.5rem 0; text-align: center; color: var(--pico-muted-color); font-size: 0.8rem; }

.pager {
  display: flex; align-items: center; justify-content: center; gap: 0.75rem;
  font-size: 0.75rem; color: var(--pico-muted-color);
  button { margin: 0; padding: 0.15rem 0.5rem; width: auto; }
}
</style>
