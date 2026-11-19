<script setup lang="ts">
import { ref, nextTick, computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import toast from '@/services/toast'

const emit = defineEmits<{ (e: 'close'): void }>()

const store = useWorkshopStore()
const { tags } = storeToRefs(store)

//* Nombre d'ateliers (de la version du jeu affichée) qui portent chaque tag.
const usage = computed(() => {
  const counts = new Map<number, number>()
  for (const workshop of store.workshops) {
    for (const tag of workshop.tags) counts.set(tag.id, (counts.get(tag.id) ?? 0) + 1)
  }
  return counts
})

/* =========================
   CREATE
========================= */
const newName = ref('')
const newColor = ref('#fbbc04')

const createTag = async () => {
  if (!newName.value.trim()) return
  try {
    await store.createTag(newName.value.trim(), newColor.value)
    newName.value = ''
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la création du tag')
  }
}

/* =========================
   EDIT
========================= */
const editingId = ref<number | null>(null)
const editName = ref('')
const editColor = ref('#fbbc04')

const startEdit = async (tag: any) => {
  editingId.value = tag.id
  editName.value = tag.name
  editColor.value = tag.color
  await nextTick()
  const el = document.getElementById(`edit-${tag.id}`) as HTMLInputElement
  el?.focus()
  el?.select()
}

const validateEdit = async (tagId: number) => {
  if (!editName.value.trim()) return
  try {
    await store.updateTag(tagId, editName.value.trim(), editColor.value)
    editingId.value = null
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la modification du tag')
  }
}

/* =========================
   DELETE
========================= */
const deleteTag = async (id: number) => {
  try {
    await store.deleteTag(id)
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la suppression du tag')
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="popup">

      <header class="popup-header">
        <h3>Mes tags</h3>
        <button type="button" class="icon-btn" title="Fermer" @click="emit('close')">
          <i class="mdi mdi-close" />
        </button>
      </header>

      <!-- CRÉATION -->
      <div class="create">
        <label class="swatch" :style="{ background: newColor }" title="Couleur">
          <input v-model="newColor" type="color">
        </label>
        <input
          v-model="newName"
          class="field"
          placeholder="Nouveau tag"
          @keyup.enter="createTag"
        />
        <button type="button" class="btn-primary" :disabled="!newName.trim()" @click="createTag">
          Ajouter
        </button>
      </div>

      <!-- LISTE -->
      <div class="tag-list">
        <div
          v-for="tag in tags"
          :key="tag.id"
          class="tag-row"
          :class="{ editing: editingId === tag.id }"
        >
          <template v-if="editingId === tag.id">
            <label class="swatch" :style="{ background: editColor }" title="Couleur">
              <input v-model="editColor" type="color">
            </label>
            <input
              :id="`edit-${tag.id}`"
              v-model="editName"
              class="field"
              @keyup.enter="validateEdit(tag.id)"
              @keyup.escape="editingId = null"
            />
            <button type="button" class="icon-btn confirm" title="Valider" @click="validateEdit(tag.id)">
              <i class="mdi mdi-check" />
            </button>
            <button type="button" class="icon-btn" title="Annuler" @click="editingId = null">
              <i class="mdi mdi-close" />
            </button>
          </template>

          <template v-else>
            <span class="dot" :style="{ background: tag.color }" />
            <span class="label">{{ tag.name }}</span>
            <span class="usage">{{ usage.get(tag.id) ?? 0 }} atelier{{ (usage.get(tag.id) ?? 0) > 1 ? 's' : '' }}</span>
            <button type="button" class="icon-btn" title="Modifier" @click="startEdit(tag)">
              <i class="mdi mdi-pencil-outline" />
            </button>
            <button type="button" class="icon-btn danger" title="Supprimer" @click="deleteTag(tag.id)">
              <i class="mdi mdi-delete-outline" />
            </button>
          </template>
        </div>

        <p v-if="!tags.length" class="empty">Aucun tag pour l'instant</p>
      </div>
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
  width: 380px;
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

//* Champs compacts : Pico les rend beaucoup trop hauts par défaut.
.field {
  flex: 1;
  min-width: 0;
  height: 2.1rem;
  margin: 0;
  padding: 0 0.65rem;
  font-size: 0.85rem;
  border-radius: 8px;
}

/* CRÉATION */
.create {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding-bottom: 0.85rem;
  margin-bottom: 0.6rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

//* Pastille de couleur : le vrai sélecteur est un input invisible posé dessus.
.swatch {
  position: relative;
  flex-shrink: 0;
  width: 1.6rem;
  height: 1.6rem;
  margin: 0;
  border-radius: 50%;
  box-shadow: 0 0 0 2px var(--pico-card-background-color), 0 0 0 3px var(--pico-muted-border-color);
  cursor: pointer;

  input {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    margin: 0;
    padding: 0;
    opacity: 0;
    cursor: pointer;
  }
}

/* LISTE */
.tag-list {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  max-height: 55vh;
  overflow-y: auto;
}

.tag-row {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  min-height: 2.4rem;
  padding: 0.25rem 0.3rem 0.25rem 0.55rem;
  border-radius: 8px;
  transition: background 0.15s;

  &:hover,
  &.editing {
    background: color-mix(in srgb, var(--pico-contrast) 6%, transparent);
  }

  &:hover .danger {
    opacity: 1;
  }
}

.dot {
  flex-shrink: 0;
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
}

.label {
  flex: 1;
  min-width: 0;
  font-size: 0.88rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.usage {
  flex-shrink: 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.empty {
  margin: 0.5rem 0 0;
  font-size: 0.8rem;
  font-style: italic;
  color: var(--pico-muted-color);
}

/* BOUTONS */
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.8rem;
  height: 1.8rem;
  margin: 0;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 1rem;
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }

  &.confirm {
    color: var(--pico-primary);
  }

  &.danger {
    opacity: 0;
    transition: opacity 0.15s;

    &:hover {
      color: #e53e3e;
    }
  }
}

.btn-primary {
  width: auto;
  margin: 0;
  padding: 0.35rem 0.85rem;
  font-size: 0.8rem;
  border-radius: 8px;
}
</style>
