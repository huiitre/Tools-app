<script setup lang="ts">
import { ref } from 'vue'
import { useR2rStore } from '../store/r2r.store'
import { R2R_SOURCES } from '../types/r2r.types'
import toast from '@/services/toast'

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'imported', id: string): void
}>()

const store = useR2rStore()

const selectedSource = ref(R2R_SOURCES[0].id)
const expeditionName = ref('')
const selectedFile = ref<File | null>(null)
const loading = ref(false)

const currentSource = () => R2R_SOURCES.find(s => s.id === selectedSource.value)

function onFileChange(e: Event) {
  const input = e.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
  if (selectedFile.value && !expeditionName.value) {
    expeditionName.value = selectedFile.value.name.replace(/\.[^.]+$/, '')
  }
}

async function submit() {
  if (!selectedFile.value) {
    toast.warning('Sélectionnez un fichier')
    return
  }
  loading.value = true
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('source', selectedSource.value)
    if (expeditionName.value.trim()) {
      formData.append('name', expeditionName.value.trim())
    }
    const id = await store.importExpedition(formData)
    toast.success('Expédition importée')
    emit('imported', id)
  } catch (e: any) {
    toast.error(e?.message || "Erreur lors de l'import")
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <div class="modal-header">
        <h3>Importer une expédition</h3>
        <span class="mdi mdi-close close-btn" @click="emit('close')" />
      </div>

      <div class="modal-body">
        <label>
          Source
          <select v-model="selectedSource">
            <option v-for="s in R2R_SOURCES" :key="s.id" :value="s.id">{{ s.label }}</option>
          </select>
        </label>

        <label>
          Fichier <small>({{ currentSource()?.accept }})</small>
          <input type="file" :accept="currentSource()?.accept" @change="onFileChange" />
        </label>

        <label>
          Nom <small>(optionnel — généré depuis le fichier si vide)</small>
          <input v-model="expeditionName" type="text" placeholder="Nom de l'expédition" />
        </label>
      </div>

      <div class="modal-footer">
        <button class="secondary" @click="emit('close')" :disabled="loading">Annuler</button>
        <button
          @click="submit"
          :aria-busy="loading"
          :disabled="!selectedFile || loading"
        >
          Importer
        </button>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  padding: 1.5rem;
  width: 100%;
  max-width: 460px;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;

  h3 { margin: 0; font-size: 1rem; }
}

.close-btn {
  font-size: 1.2rem;
  cursor: pointer;
  color: var(--pico-muted-color);
  &:hover { color: var(--pico-color); }
}

.modal-body {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;

  label {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    font-size: 0.875rem;
    font-weight: 500;

    small {
      font-weight: 400;
      color: var(--pico-muted-color);
    }
  }

  input, select { margin: 0; }
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;

  button { margin: 0; width: auto; }
}
</style>
