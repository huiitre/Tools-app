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

      <header class="popup-header">
        <h3>Modifier l'atelier</h3>
        <button type="button" class="icon-btn" title="Fermer" @click="emit('close')">
          <i class="mdi mdi-close" />
        </button>
      </header>

      <!-- NOM -->
      <label class="section-label" for="edit-workshop-name">Nom</label>
      <input
        id="edit-workshop-name"
        v-model="name"
        class="field"
        placeholder="Nom de l'atelier"
        @keyup.enter="save"
      />

      <!-- LIENS -->
      <p class="section-label">
        Liens
        <span class="section-count">{{ links.length }} / 3</span>
      </p>

      <div class="links-list">
        <template v-for="link in links" :key="link.id">

          <!-- Lecture -->
          <div v-if="editingId !== link.id" class="link-row">
            <span class="source-badge">{{ link.source === WorkshopLinkSource.DOFUSBOOK ? 'Dofusbook' : 'Perso' }}</span>
            <div class="link-text">
              <span class="link-label">{{ link.label }}</span>
              <span class="link-url">{{ link.url }}</span>
            </div>
            <button type="button" class="icon-btn" title="Modifier le lien" @click="startEditLink(link)">
              <i class="mdi mdi-pencil-outline" />
            </button>
            <button type="button" class="icon-btn danger" title="Supprimer le lien" @click="deleteLink(link.id)">
              <i class="mdi mdi-delete-outline" />
            </button>
          </div>

          <!-- Édition -->
          <div v-else class="link-form">
            <input v-model="editLabel" class="field" placeholder="Libellé" @keyup.enter="saveEdit(link)" />
            <input v-model="editUrl" class="field" placeholder="URL" @keyup.enter="saveEdit(link)" />
            <div class="form-actions">
              <button type="button" class="btn-ghost" @click="cancelEdit">Annuler</button>
              <button type="button" class="btn-primary" :aria-busy="editLoading" @click="saveEdit(link)">Valider</button>
            </div>
          </div>

        </template>

        <!-- Ajout -->
        <div v-if="showAddForm" class="link-form">
          <div class="add-row">
            <select v-model="newSource" class="field source-select">
              <option :value="WorkshopLinkSource.DOFUSBOOK">Dofusbook</option>
              <option :value="WorkshopLinkSource.CUSTOM">Perso</option>
            </select>
            <input v-model="newUrl" class="field" placeholder="URL" @keyup.enter="confirmAdd" />
          </div>
          <div class="form-actions">
            <button type="button" class="btn-ghost" @click="cancelAdd">Annuler</button>
            <button type="button" class="btn-primary" :aria-busy="addLoading" @click="confirmAdd">Ajouter</button>
          </div>
        </div>

        <p v-if="!links.length && !showAddForm" class="links-empty">Aucun lien</p>
      </div>

      <button
        v-if="!showAddForm"
        type="button"
        class="add-link-btn"
        :disabled="links.length >= 3"
        @click="openAddForm"
      >
        <i class="mdi mdi-plus" />
        Ajouter un lien
      </button>

      <footer class="popup-footer">
        <button type="button" class="btn-primary" @click="save">Enregistrer</button>
      </footer>

    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  width: 420px;
  max-width: calc(100vw - 2rem);
  padding: 1rem 1.1rem 1.1rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, .35);
}

.popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.9rem;

  h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--pico-contrast);
  }
}

.section-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 0 0 0.4rem;
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.section-count {
  font-weight: 500;
  letter-spacing: 0;
}

//* Champs compacts : Pico les rend beaucoup trop hauts par défaut.
.field {
  width: 100%;
  height: 2.2rem;
  margin: 0;
  padding: 0 0.7rem;
  font-size: 0.85rem;
  border-radius: 8px;
}

#edit-workshop-name {
  margin-bottom: 1.1rem;
}

/* LIENS */
.links-list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 0.5rem;
}

.link-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.45rem 0.4rem 0.45rem 0.55rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-contrast) 5%, transparent);
}

.source-badge {
  flex-shrink: 0;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 18%, transparent);
  color: var(--pico-primary);
  font-size: 0.65rem;
  font-weight: 700;
}

.link-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  line-height: 1.25;
}

.link-label,
.link-url {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.link-label {
  font-size: 0.85rem;
  color: var(--pico-contrast);
}

.link-url {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.link-form {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.55rem;
  border-radius: 8px;
  border: 1px solid var(--pico-card-border-color);
  background: color-mix(in srgb, var(--pico-contrast) 3%, transparent);
}

.add-row {
  display: flex;
  gap: 0.4rem;
}

.source-select {
  width: auto;
  flex-shrink: 0;
  padding-right: 2rem;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.4rem;
}

.links-empty {
  margin: 0;
  font-size: 0.8rem;
  font-style: italic;
  color: var(--pico-muted-color);
}

.add-link-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  width: 100%;
  margin: 0;
  padding: 0.45rem;
  font-size: 0.8rem;
  background: transparent;
  border: 1px dashed var(--pico-muted-border-color);
  border-radius: 8px;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: border-color 0.15s, color 0.15s;

  &:hover:not(:disabled) {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
}

.popup-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 1rem;
}

/* BOUTONS */
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.9rem;
  height: 1.9rem;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 1.05rem;
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }

  &.danger:hover {
    color: #e53e3e;
  }
}

.btn-primary,
.btn-ghost {
  width: auto;
  margin: 0;
  padding: 0.35rem 0.9rem;
  font-size: 0.8rem;
  border-radius: 8px;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  color: inherit;

  &:hover {
    border-color: var(--pico-contrast);
  }
}
</style>
