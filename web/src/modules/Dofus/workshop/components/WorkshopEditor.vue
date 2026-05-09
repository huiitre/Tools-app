<script setup lang="ts">
import { ref, nextTick } from 'vue'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import toast from '@/services/toast'
import { Workshop, WorkshopLink, WorkshopLinkSource } from '@/modules/Dofus/workshop/types/workshop.types'
import {
  useAddWorkshopLink,
  useUpdateWorkshopLink,
  useDeleteWorkshopLink,
} from '@/modules/Dofus/workshop/fetch/workshopLink.fetch'

const props = defineProps<{ workshop: Workshop }>()
const emit = defineEmits<{ (e: 'close'): void }>()

const store = useWorkshopStore()

/* =========================
   NOM
========================= */
const name = ref(props.workshop.name)

const focusNameInput = async () => {
  await nextTick()
  const el = document.getElementById('edit-workshop-name') as HTMLInputElement
  el?.focus()
  el?.select()
}
focusNameInput()

const save = async () => {
  if (!name.value.trim()) return
  try {
    await store.updateWorkshop(props.workshop.id, {
      name: name.value.trim(),
      active: props.workshop.active,
    })
    emit('close')
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la modification')
  }
}

/* =========================
   LIENS
========================= */
const links = ref<WorkshopLink[]>([...(props.workshop.links ?? [])])

const syncToStore = () => {
  const w = store.workshops.find(w => w.id === props.workshop.id)
  if (w) w.links = [...links.value]
}

// --- Ajout ---
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

const cancelAdd = () => {
  showAddForm.value = false
}

const confirmAdd = async () => {
  if (!newUrl.value.trim() || addLoading.value) return
  addLoading.value = true
  try {
    const res = await useAddWorkshopLink(props.workshop.id, {
      url: newUrl.value.trim(),
      source: newSource.value,
    })
    links.value.push(res.data)
    syncToStore()
    showAddForm.value = false
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de l\'ajout du lien')
  } finally {
    addLoading.value = false
  }
}

// --- Édition ---
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

const cancelEdit = () => {
  editingId.value = null
}

const saveEdit = async (link: WorkshopLink) => {
  if (!editUrl.value.trim() || editLoading.value) return
  editLoading.value = true
  try {
    const res = await useUpdateWorkshopLink(props.workshop.id, link.id, {
      url: editUrl.value.trim(),
      label: editLabel.value.trim(),
    })
    const idx = links.value.findIndex(l => l.id === link.id)
    if (idx !== -1) links.value[idx] = res.data
    syncToStore()
    editingId.value = null
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la modification du lien')
  } finally {
    editLoading.value = false
  }
}

// --- Suppression ---
const deleteLink = async (linkId: number) => {
  try {
    await useDeleteWorkshopLink(props.workshop.id, linkId)
    links.value = links.value.filter(l => l.id !== linkId)
    syncToStore()
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la suppression du lien')
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="popup">

      <!-- NOM -->
      <p class="section-label">Nom de l'atelier</p>
      <div class="row">
        <input
          id="edit-workshop-name"
          v-model="name"
          placeholder="Nom de l'atelier"
          @keyup.enter="save"
        />
        <span class="mdi mdi-check action" @click="save"></span>
      </div>

      <!-- SÉPARATEUR -->
      <div class="divider"></div>

      <!-- LIENS -->
      <p class="section-label">Liens</p>

      <div class="links-list">

        <!-- Lien existant -->
        <template v-for="link in links" :key="link.id">

          <!-- Mode lecture -->
          <div v-if="editingId !== link.id" class="link-row">
            <span class="mdi mdi-link-variant link-icon"></span>
            <span class="link-label">{{ link.label }}</span>
            <span class="mdi mdi-pencil-outline action" @click="startEditLink(link)"></span>
            <span class="mdi mdi-delete-outline action" @click="deleteLink(link.id)"></span>
          </div>

          <!-- Mode édition -->
          <div v-else class="link-edit">
            <input v-model="editUrl" placeholder="URL" class="link-input" />
            <div class="link-edit-row">
              <input v-model="editLabel" placeholder="Label" class="link-input" @keyup.enter="saveEdit(link)" />
              <span class="mdi mdi-check action" @click="saveEdit(link)"></span>
              <span class="mdi mdi-close action" @click="cancelEdit"></span>
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
            <span class="mdi mdi-check action" @click="confirmAdd"></span>
            <span class="mdi mdi-close action" @click="cancelAdd"></span>
          </div>
        </div>

      </div>

      <!-- BOUTON AJOUT -->
      <button
        v-if="!showAddForm"
        class="add-link-btn"
        :disabled="links.length >= 3"
        @click="openAddForm"
      >
        <span class="mdi mdi-plus"></span>
        Ajouter un lien
      </button>

    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  width: 380px;
  padding: 0.75rem;
}

.section-label {
  margin: 0 0 0.35rem 0.25rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.divider {
  border-top: 1px solid var(--pico-muted-border-color);
  margin: 0.75rem 0;
}

/* Ligne nom */
.row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.2rem 0.25rem;

  input {
    flex: 1;
    border: none;
    background: transparent;
    color: var(--pico-color);
    font-size: 0.9rem;
    margin: 0;
  }
}

/* Liste des liens */
.links-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.5rem;
}

/* Lien en lecture */
.link-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.25rem;
  border-radius: var(--pico-border-radius);

  &:hover {
    background: var(--pico-muted-border-color);
  }
}

.link-icon {
  font-size: 0.9rem;
  color: var(--pico-muted-color);
  flex-shrink: 0;
}

.link-label {
  flex: 1;
  font-size: 0.85rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Lien en édition / ajout */
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
  font-size: 0.85rem;
  padding: 0.1rem 0;
  margin: 0;
}

.source-select {
  flex-shrink: 0;
  font-size: 0.8rem;
  padding: 0.1rem 0.3rem;
  height: auto;
  width: auto;
  margin: 0;
}

/* Bouton ajout */
.add-link-btn {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.5rem;
  font-size: 0.8rem;
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
    font-size: 0.9rem;
  }
}

.action {
  font-size: 1.1rem;
  cursor: pointer;
  opacity: 0.6;
  flex-shrink: 0;

  &:hover {
    opacity: 1;
  }
}
</style>
