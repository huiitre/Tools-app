<script setup lang="ts">
import { ref, nextTick } from 'vue'
import { storeToRefs } from 'pinia'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import toast from '@/services/toast'

const emit = defineEmits<{ (e: 'close'): void }>()

const store = useWorkshopStore()
const { tags } = storeToRefs(store)

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
      <!-- CREATE -->
      <div class="row create">
        <input
          v-model="newName"
          placeholder="Créer un libellé"
          @keyup.enter="createTag"
        />

        <input
          class="color"
          type="color"
          v-model="newColor"
        />

        <span
          class="mdi mdi-check action"
          @click="createTag"
        ></span>
      </div>

      <!-- LIST -->
      <div
        v-for="tag in tags"
        :key="tag.id"
        class="row"
      >
        <span class="mdi mdi-tag icon"></span>

        <template v-if="editingId === tag.id">
          <input
            :id="`edit-${tag.id}`"
            v-model="editName"
            @keyup.enter="validateEdit(tag.id)"
          />
        </template>

        <template v-else>
          <span class="label">{{ tag.name }}</span>
        </template>

        <input
          v-if="editingId === tag.id"
          class="color"
          type="color"
          v-model="editColor"
        />

        <span
          v-if="editingId === tag.id"
          class="mdi mdi-check action"
          @click="validateEdit(tag.id)"
        ></span>

        <span
          v-else
          class="mdi mdi-pencil action"
          @click="startEdit(tag)"
        ></span>

        <span
          class="mdi mdi-delete action delete"
          @click="deleteTag(tag.id)"
        ></span>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  width: 320px;
  padding: 0.5rem;
}

.row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.45rem;

  &:hover .delete {
    opacity: 1;
  }

  input {
    border: none;
    background: transparent;
    color: var(--pico-color);
    font-size: 0.9rem;
    margin: 0;
  }

  .label {
    flex: 1;
    font-size: 0.9rem;
  }
}

.create {
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.icon {
  opacity: 0.6;
}

.action {
  font-size: 1.2rem;
  cursor: pointer;
  opacity: 0.7;

  &:hover {
    opacity: 1;
  }
}

.delete {
  font-size: 1.1rem;
  cursor: pointer;
  opacity: 0;
  color: var(--pico-del-color);
}

.color {
  width: 28px;
  height: 20px;
  padding: 0;
  border: none;
  background: none;
  cursor: pointer;
}
</style>
