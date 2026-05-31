<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useR2rStore } from './store/r2r.store'
import ExpeditionList from './components/ExpeditionList.vue'
import ExpeditionTracker from './components/ExpeditionTracker.vue'
import ImportModal from './components/ImportModal.vue'
import toast from '@/services/toast'

const store = useR2rStore()

const showImportModal = ref(false)
const view = computed(() => store.currentExpedition ? 'tracker' : 'list')

onMounted(async () => {
  try {
    await store.fetchExpeditions()
  } catch {
    toast.error('Erreur lors du chargement des expéditions')
  }
})

async function onSelect(id: string) {
  try {
    await store.loadExpedition(id)
  } catch {
    toast.error("Erreur lors du chargement de l'expédition")
  }
}

async function onDelete(id: string) {
  if (!confirm('Supprimer cette expédition ?')) return
  try {
    await store.removeExpedition(id)
    toast.success('Expédition supprimée')
  } catch {
    toast.error('Erreur lors de la suppression')
  }
}

async function onImported(id: string) {
  showImportModal.value = false
  await onSelect(id)
}
</script>

<template>
  <div class="r2r-page">
    <Transition name="fade" mode="out-in">
      <ExpeditionTracker v-if="view === 'tracker'" :key="store.currentExpedition?.id" />
      <ExpeditionList
        v-else
        :expeditions="store.expeditions"
        :loading="store.loading"
        @select="onSelect"
        @delete="onDelete"
        @import="showImportModal = true"
      />
    </Transition>

    <ImportModal
      v-if="showImportModal"
      @close="showImportModal = false"
      @imported="onImported"
    />
  </div>
</template>

<style lang="scss" scoped>
.r2r-page {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>