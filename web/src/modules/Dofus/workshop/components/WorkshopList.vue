<script setup lang="ts">
import { ref, computed, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { Workshop } from '@/modules/Dofus/workshop/types/workshop.types'
import WorkshopTagSelector from '@/modules/Dofus/workshop/components/WorkshopTagSelector.vue'
import WorkshopEditor from '@/modules/Dofus/workshop/components/WorkshopEditor.vue'
import WorkshopLinkViewer from '@/modules/Dofus/workshop/components/WorkshopLinkViewer.vue'
import WorkshopListFilters from '@/modules/Dofus/workshop/components/WorkshopListFilters.vue'
import WorkshopCard from '@/modules/Dofus/workshop/components/WorkshopCard.vue'
import WorkshopDeleteConfirm from '@/modules/Dofus/workshop/components/WorkshopDeleteConfirm.vue'

const router = useRouter()
const store = useWorkshopStore()

const route = useRoute()

const selectedWorkshop = ref<Workshop | null>(null)
const editedWorkshop = ref<Workshop | null>(null)
const deletedWorkshop = ref<Workshop | null>(null)
const activeLink = ref<{ url: string; label: string } | null>(null)

//* Archive ou ateliers actifs, avant filtre : base du compteur.
const scopedWorkshops = computed(() =>
  store.workshops.filter(w => route.name === 'dofus-workshop-archive' ? !w.active : w.active)
)

const matchesAny = (selected: Set<number>, items: { id: number }[]) =>
  !selected.size || items.some(item => selected.has(item.id))

//* Au moins un des éléments choisis dans chaque famille (tags, classes, métiers).
const matchesFilters = (workshop: Workshop) =>
  matchesAny(store.selectedTagIds, workshop.tags)
  && matchesAny(store.selectedBreedIds, workshop.breeds)
  && matchesAny(store.selectedJobIds, workshop.jobs)

const newWorkshop = computed(() =>
  store.workshops.find(w => w.id === store.newWorkshopId) ?? null
)

//* L'atelier tout juste créé passe devant tout, filtres compris ; puis épinglés, puis le tri choisi.
const workshops = computed(() => {
  const direction = store.sortDir === 'asc' ? 1 : -1
  const sorted = scopedWorkshops.value
    .filter(w => w.id !== store.newWorkshopId && matchesFilters(w))
    .sort((a, b) => {
      if (a.pinned !== b.pinned) return a.pinned ? -1 : 1
      return direction * a.name.localeCompare(b.name)
    })
  return newWorkshop.value ? [newWorkshop.value, ...sorted] : sorted
})

//* Quitter la liste (atelier ouvert, autre page) lui rend sa place.
onBeforeUnmount(() => {
  store.newWorkshopId = null
})

/* =========================
   NAVIGATION
========================= */
const openWorkshop = (workshop: Workshop) => {
  router.push({
    name: 'dofus-workshop-detail',
    params: { workshopId: workshop.id }
  })
}

/* =========================
   ACTIONS
========================= */
const archive = async (id: number, workshop: Workshop) => {
  await store.updateWorkshop(id, {
    active: !workshop.active,
    name: workshop.name
  })
}


const openTagSelector = (workshop: Workshop) => {
  selectedWorkshop.value = workshop
}

const openEditor = (workshop: Workshop) => {
  editedWorkshop.value = workshop
}

const pinWorkshop = async (workshop: Workshop) => {
  await store.updateWorkshop(workshop.id, {
    pinned: !workshop.pinned,
    name: workshop.name
  })
}
</script>

<template>
  <WorkshopListFilters
    :visible-count="workshops.length"
    :total-count="scopedWorkshops.length"
    :workshops="scopedWorkshops"
  />

  <div class="workshops-grid">
    <WorkshopCard
      v-for="workshop in workshops"
      :key="workshop.id"
      :workshop="workshop"
      :is-new="workshop.id === store.newWorkshopId"
      @open="openWorkshop(workshop)"
      @pin="pinWorkshop(workshop)"
      @edit="openEditor(workshop)"
      @archive="archive(workshop.id, workshop)"
      @remove="deletedWorkshop = workshop"
      @edit-tags="openTagSelector(workshop)"
      @open-link="link => activeLink = { url: link.url, label: link.label }"
    />
  </div>

  <div
    v-if="workshops.length === 0"
    class="empty-state"
  >
    <span class="mdi mdi-archive empty-icon"></span>
    <p class="empty-text">Aucun atelier à afficher</p>
  </div>

  <WorkshopTagSelector
    v-if="selectedWorkshop"
    :workshop="selectedWorkshop"
    @close="selectedWorkshop = null"
  />

  <WorkshopDeleteConfirm
    v-if="deletedWorkshop"
    :workshop="deletedWorkshop"
    @close="deletedWorkshop = null"
  />

  <WorkshopEditor
    v-if="editedWorkshop"
    :workshop="editedWorkshop"
    @close="editedWorkshop = null"
  />

  <WorkshopLinkViewer
    v-if="activeLink"
    :url="activeLink.url"
    :label="activeLink.label"
    @close="activeLink = null"
  />
</template>

<style scoped lang="scss">
.workshops-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 1rem;
}

/* EMPTY */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
  opacity: 0.5;
  color: var(--pico-muted-color);
}

.empty-text {
  color: var(--pico-muted-color);
  font-size: 1.1rem;
}
</style>
