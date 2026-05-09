<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, watch } from 'vue'
import { useFloating, offset } from '@floating-ui/vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import CataloguePreferencesPanel from '@/modules/Dofus/catalogue/components/CataloguePreferencesPanel.vue'
import CatalogueBatchActionsMenu from '@/modules/Dofus/catalogue/components/CatalogueBatchActionsMenu.vue'
import AddToWorkshopModal from '@/modules/Dofus/catalogue/components/AddToWorkshopModal.vue'

const catalogueStore = useCatalogueStore()

/* ======================
   SEARCH
====================== */

const searchValue = ref(catalogueStore.q ?? '')
let debounceTimer: ReturnType<typeof setTimeout> | null = null

watch(searchValue, (value) => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    catalogueStore.setQuery(value.trim() || null)
  }, 300)
})

onBeforeUnmount(() => {
  if (debounceTimer) clearTimeout(debounceTimer)
})

/* ======================
   PAGINATION (STORE)
====================== */

const page = computed(() => catalogueStore.page)
const lastPage = computed(() => catalogueStore.lastPage)

const hasPrevious = computed(() => catalogueStore.previousPage !== null)
const hasNext = computed(() => catalogueStore.nextPage !== null)

const goFirst = () => {
  if (page.value !== 1) catalogueStore.setPage(1)
}

const goPrevious = () => {
  if (catalogueStore.previousPage !== null) {
    catalogueStore.setPage(catalogueStore.previousPage)
  }
}

const goNext = () => {
  if (catalogueStore.nextPage !== null) {
    catalogueStore.setPage(catalogueStore.nextPage)
  }
}

const goLast = () => {
  if (
    catalogueStore.lastPage !== null &&
    page.value !== catalogueStore.lastPage
  ) {
    catalogueStore.setPage(catalogueStore.lastPage)
  }
}

/* ======================
   PREFERENCES FLOATING UI
====================== */

const isPreferencesOpen = ref(false)
const preferencesRef = ref<HTMLElement | null>(null)
const preferencesFloating = ref<HTMLElement | null>(null)

const { floatingStyles: preferencesStyles } = useFloating(preferencesRef, preferencesFloating, {
  placement: 'right-start',
  middleware: [offset(6)],
})

/* ======================
   BATCH ACTIONS FLOATING UI
====================== */

const isBatchOpen = ref(false)
const batchRef = ref<HTMLElement | null>(null)
const batchFloating = ref<HTMLElement | null>(null)

const { floatingStyles: batchStyles } = useFloating(batchRef, batchFloating, {
  placement: 'right-start',
  middleware: [offset(6)],
})

const hasSelection = computed(() => catalogueStore.selectedItemsWithRecipe.length > 0)

/* ======================
   ADD TO WORKSHOP MODAL
====================== */

const showAddToWorkshopModal = ref(false)

function handleAddToWorkshop() {
  isBatchOpen.value = false
  showAddToWorkshopModal.value = true
}

/* ======================
   CLICK OUTSIDE & SCROLL
====================== */

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  
  // Preferences
  if (
    preferencesRef.value &&
    preferencesFloating.value &&
    !preferencesRef.value.contains(target) &&
    !preferencesFloating.value.contains(target)
  ) {
    isPreferencesOpen.value = false
  }

  // Batch
  if (
    batchRef.value &&
    batchFloating.value &&
    !batchRef.value.contains(target) &&
    !batchFloating.value.contains(target)
  ) {
    isBatchOpen.value = false
  }
}

const onScroll = () => {
  isPreferencesOpen.value = false
  isBatchOpen.value = false
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('scroll', onScroll, true)
})
</script>

<template>
  <div class="catalogue-toolbar">
    <!-- LEFT -->
    <div class="toolbar-left">
      <button
        class="icon"
        :disabled="page === 1"
        @click="goFirst"
      >
        <i class="mdi mdi-page-first" />
      </button>

      <button
        class="icon"
        :disabled="!hasPrevious"
        @click="goPrevious"
      >
        <i class="mdi mdi-chevron-left" />
      </button>

      <span class="page-indicator">
        Page <strong>{{ page }}</strong>
        <span v-if="lastPage"> / {{ lastPage }}</span>
      </span>

      <button
        class="icon"
        :disabled="!hasNext"
        @click="goNext"
      >
        <i class="mdi mdi-chevron-right" />
      </button>

      <button
        class="icon"
        :disabled="!hasNext"
        @click="goLast"
      >
        <i class="mdi mdi-page-last" />
      </button>

      <!-- PREFERENCES -->
      <button
        ref="preferencesRef"
        class="icon preferences"
        aria-label="Préférences catalogue"
        @click="isPreferencesOpen = !isPreferencesOpen"
      >
        <i class="mdi mdi-tune" />
      </button>

      <div
        v-if="isPreferencesOpen"
        ref="preferencesFloating"
        class="floating-panel"
        :style="preferencesStyles"
      >
        <CataloguePreferencesPanel />
      </div>

      <!-- BATCH ACTIONS -->
      <button
        ref="batchRef"
        class="icon batch"
        :disabled="!hasSelection"
        aria-label="Actions groupées"
        @click="isBatchOpen = !isBatchOpen"
      >
        <i class="mdi mdi-playlist-check" />
      </button>

      <div
        v-if="isBatchOpen"
        ref="batchFloating"
        class="floating-panel"
        :style="batchStyles"
      >
        <CatalogueBatchActionsMenu
          @add-to-workshop="handleAddToWorkshop"
        />
      </div>
    </div>

    <!-- RIGHT -->
    <div class="toolbar-right">
      <input
        type="search"
        placeholder="Rechercher un objet…"
        v-model="searchValue"
      />
    </div>
  </div>

  <!-- MODAL -->
  <AddToWorkshopModal
    v-if="showAddToWorkshopModal"
    :item-ids="catalogueStore.selectedItemsWithRecipe"
    @close="showAddToWorkshopModal = false"
  />
</template>

<style scoped lang="scss">
.catalogue-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.25rem 0;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

button {
  background: none;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;
}

button:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

button:hover:not(:disabled) {
  color: var(--pico-primary);
}

button.icon {
  padding: 0.25rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

button.icon i {
  font-size: 1.1rem;
}

.page-indicator {
  margin: 0 0.5rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  user-select: none;
}

.preferences,
.batch {
  margin-left: 0.5rem;
}

.toolbar-right input[type='search'] {
  min-width: 520px;
}

.floating-panel {
  position: absolute;
  z-index: 1000;
  padding: 0.5rem 0.65rem;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
}
</style>