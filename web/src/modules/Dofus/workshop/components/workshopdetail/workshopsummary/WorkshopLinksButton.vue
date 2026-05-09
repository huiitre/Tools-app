<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useFloating, offset } from '@floating-ui/vue'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { WorkshopLink, WorkshopLinkSource } from '@/modules/Dofus/workshop/types/workshop.types'
import {
  useAddWorkshopLink,
  useUpdateWorkshopLink,
  useDeleteWorkshopLink,
} from '@/modules/Dofus/workshop/fetch/workshopLink.fetch'
import toast from '@/services/toast'
import WorkshopLinkViewer from '@/modules/Dofus/workshop/components/WorkshopLinkViewer.vue'

const workshopDetailStore = useWorkshopDetailStore()
const workshopStore = useWorkshopStore()

const workshop = computed(() =>
  workshopStore.workshops.find(w => w.id === workshopDetailStore.workshopId)
)

const links = computed(() => workshop.value?.links ?? [])

/* ========================= FLOATING ========================= */
const isOpen = ref(false)
const reference = ref<HTMLElement | null>(null)
const floating = ref<HTMLElement | null>(null)

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-end',
  middleware: [offset(5)],
})

const closePanel = () => {
  isOpen.value = false
  editingId.value = null
  showAddForm.value = false
}

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (
    reference.value &&
    floating.value &&
    !reference.value.contains(target) &&
    !floating.value.contains(target)
  ) {
    closePanel()
  }
}

const onScroll = (e: Event) => {
  if (isOpen.value && !floating.value?.contains(e.target as Node)) closePanel()
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('scroll', onScroll, true)
})
onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('scroll', onScroll, true)
})

/* ========================= VIEWER ========================= */
const activeLink = ref<{ url: string; label: string } | null>(null)

const openLink = (link: WorkshopLink) => {
  closePanel()
  activeLink.value = { url: link.url, label: link.label }
}

/* ========================= AJOUT ========================= */
const showAddForm = ref(false)
const newSource = ref<WorkshopLinkSource>(WorkshopLinkSource.DOFUSBOOK)
const newUrl = ref('')
const addLoading = ref(false)

const openAddForm = () => {
  editingId.value = null
  newUrl.value = ''
  newSource.value = WorkshopLinkSource.DOFUSBOOK
  showAddForm.value = true
}

const cancelAdd = () => { showAddForm.value = false }

const confirmAdd = async () => {
  if (!newUrl.value.trim() || addLoading.value || !workshop.value) return
  addLoading.value = true
  try {
    const res = await useAddWorkshopLink(workshop.value.id, {
      url: newUrl.value.trim(),
      source: newSource.value,
    })
    if (workshop.value) {
      workshop.value.links = [...(workshop.value.links ?? []), res.data]
    }
    showAddForm.value = false
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : "Erreur lors de l'ajout du lien")
  } finally {
    addLoading.value = false
  }
}

/* ========================= ÉDITION ========================= */
const editingId = ref<number | null>(null)
const editUrl = ref('')
const editLabel = ref('')
const editLoading = ref(false)

const startEditLink = (link: WorkshopLink) => {
  showAddForm.value = false
  editingId.value = link.id
  editUrl.value = link.url
  editLabel.value = link.label
}

const cancelEdit = () => { editingId.value = null }

const saveEdit = async (link: WorkshopLink) => {
  if (!editUrl.value.trim() || editLoading.value || !workshop.value) return
  editLoading.value = true
  try {
    const res = await useUpdateWorkshopLink(workshop.value.id, link.id, {
      url: editUrl.value.trim(),
      label: editLabel.value.trim(),
    })
    const idx = workshop.value.links?.findIndex(l => l.id === link.id) ?? -1
    if (idx !== -1) workshop.value.links![idx] = res.data
    editingId.value = null
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la modification du lien')
  } finally {
    editLoading.value = false
  }
}

/* ========================= SUPPRESSION ========================= */
const deleteLink = async (linkId: number) => {
  if (!workshop.value) return
  try {
    await useDeleteWorkshopLink(workshop.value.id, linkId)
    if (workshop.value.links) {
      workshop.value.links = workshop.value.links.filter(l => l.id !== linkId)
    }
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la suppression du lien')
  }
}
</script>

<template>
  <div class="links-button-wrapper">
    <button
      ref="reference"
      type="button"
      class="links-btn"
      title="Liens de l'atelier"
      @click="isOpen = !isOpen"
    >
      <span class="mdi mdi-link-variant"></span>
      <span v-if="links.length" class="links-count">{{ links.length }}</span>
    </button>

    <div
      v-if="isOpen"
      ref="floating"
      :style="floatingStyles"
      class="floating-panel"
    >
      <p class="section-label">Liens</p>

      <div class="links-list">

        <!-- Lien existant -->
        <template v-for="link in links" :key="link.id">

          <!-- Mode lecture -->
          <div v-if="editingId !== link.id" class="link-row" @click.stop="openLink(link)">
            <span class="mdi mdi-link-variant link-icon"></span>
            <span class="link-label">{{ link.label }}</span>
            <span class="mdi mdi-pencil-outline action" @click.stop="startEditLink(link)"></span>
            <span class="mdi mdi-delete-outline action" @click.stop="deleteLink(link.id)"></span>
          </div>

          <!-- Mode édition -->
          <div v-else class="link-edit">
            <input v-model="editUrl" placeholder="URL" class="link-input" />
            <div class="link-edit-row">
              <input
                v-model="editLabel"
                placeholder="Label"
                class="link-input"
                @keyup.enter="saveEdit(link)"
              />
              <span class="mdi mdi-check action" @click.stop="saveEdit(link)"></span>
              <span class="mdi mdi-close action" @click.stop="cancelEdit"></span>
            </div>
          </div>

        </template>

        <!-- Formulaire d'ajout -->
        <div v-if="showAddForm" class="link-edit">
          <div class="link-add-row">
            <select v-model="newSource" class="source-select">
              <option :value="WorkshopLinkSource.DOFUSBOOK">Dofusbook</option>
              <option :value="WorkshopLinkSource.CUSTOM">Personnalisé</option>
            </select>
            <input
              v-model="newUrl"
              placeholder="URL"
              class="link-input"
              @keyup.enter="confirmAdd"
            />
          </div>
          <div class="link-edit-actions">
            <span class="mdi mdi-check action" @click.stop="confirmAdd"></span>
            <span class="mdi mdi-close action" @click.stop="cancelAdd"></span>
          </div>
        </div>

      </div>

      <button
        v-if="!showAddForm"
        class="add-link-btn"
        :disabled="links.length >= 3"
        @click.stop="openAddForm"
      >
        <span class="mdi mdi-plus"></span>
        Ajouter un lien
      </button>
    </div>
  </div>

  <WorkshopLinkViewer
    v-if="activeLink"
    :url="activeLink.url"
    :label="activeLink.label"
    @close="activeLink = null"
  />
</template>

<style scoped lang="scss">
.links-button-wrapper {
  display: inline-flex;
  position: relative;
}

/* ---- Bouton ---- */
.links-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  margin: 0;
  height: 2rem;
  padding: 0 0.4rem;
  background: var(--pico-form-element-background-color);
  border: 1px solid var(--pico-form-element-border-color);
  color: var(--pico-form-element-color);
  font-size: 0.9rem;
  cursor: pointer;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

.links-count {
  font-size: 0.7rem;
  font-weight: 600;
  line-height: 1;
}

/* ---- Panel flottant ---- */
.floating-panel {
  position: absolute;
  z-index: 1000;
  width: 300px;
  padding: 0.6rem 0.65rem;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  font-size: 0.75rem;
  color: var(--pico-color);
}

.section-label {
  margin: 0 0 0.4rem 0.1rem;
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* ---- Liste liens ---- */
.links-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.5rem;
}

.link-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.25rem;
  border-radius: var(--pico-border-radius);
  cursor: pointer;

  &:hover {
    background: var(--pico-muted-border-color);

    .link-label,
    .link-icon {
      color: var(--pico-primary);
    }
  }
}

.link-icon {
  font-size: 0.9rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
}

.link-label {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ---- Édition / ajout ---- */
.link-edit {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  padding: 0.35rem 0.25rem;
  background: var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
}

.link-add-row {
  display: flex;
  gap: 0.4rem;
}

.link-edit-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.link-edit-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.4rem;
}

.link-input {
  flex: 1;
  border: none;
  border-bottom: 1px solid var(--pico-muted-color);
  background: transparent;
  color: var(--pico-color);
  font-size: 0.8rem;
  padding: 0.1rem 0;
  margin: 0;
}

.source-select {
  flex-shrink: 0;
  font-size: 0.75rem;
  padding: 0.1rem 0.3rem;
  height: auto;
  width: auto;
  margin: 0;
}

/* ---- Bouton ajout ---- */
.add-link-btn {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.5rem;
  font-size: 0.75rem;
  background: transparent;
  border: 2px dashed var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  color: var(--pico-muted-color);
  cursor: pointer;
  width: 100%;
  justify-content: center;
  transition: border-color 0.15s, color 0.15s;

  &:hover:not(:disabled) {
    border-color: var(--pico-primary-border);
    color: var(--pico-primary);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .mdi {
    font-size: 0.85rem;
  }
}

.action {
  font-size: 1rem;
  cursor: pointer;
  opacity: 0.6;
  flex-shrink: 0;

  &:hover {
    opacity: 1;
  }
}
</style>
