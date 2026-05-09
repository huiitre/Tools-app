<script setup lang="ts">
import { ref } from 'vue'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import toast from '@/services/toast'

const workshopStore = useWorkshopStore()
const workshopName = ref('')
const isCreating = ref(false)

const createWorkshop = async () => {
  const trimmedName = workshopName.value.trim()

  if (trimmedName.length < 3) {
    toast.error('Le nom doit contenir au moins 3 caractères')
    return
  }

  if (trimmedName.length > 30) {
    toast.error('Le nom ne peut pas dépasser 30 caractères')
    return
  }

  isCreating.value = true

  try {
    await workshopStore.createWorkshop(trimmedName)
    workshopName.value = ''
    toast.success('Atelier créé avec succès')
  } catch (error: any) {
    toast.error(
      error instanceof Error
        ? error.message
        : 'Erreur lors de la création'
    )
  } finally {
    isCreating.value = false
  }
}

const handleKeyPress = (event: KeyboardEvent) => {
  if (event.key === 'Enter') {
    createWorkshop()
  }
}
</script>

<template>
  <div>
    <div class="create-bar">
      <input
        v-model="workshopName"
        type="text"
        placeholder="Créer un atelier..."
        class="create-input"
        :disabled="isCreating"
        @keypress="handleKeyPress"
      />
      <button
        class="btn-create"
        :disabled="isCreating"
        @click="createWorkshop"
      >
        {{ isCreating ? 'Création...' : 'Créer' }}
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.create-bar {
  display: flex;
  gap: 1rem;
  max-width: 600px;
  margin: 0 auto 2rem;
  align-items: stretch;
}

.create-input,
.btn-create {
  padding: 0.75rem 1rem;
  border-radius: var(--pico-border-radius);
  font-size: 1rem;
  line-height: 1.5;
  border: none;
  margin: 0;

  &:disabled {
    opacity: var(--pico-form-element-disabled-opacity);
    cursor: not-allowed;
  }
}

.create-input {
  flex: 1;
  border: 1px solid var(--pico-form-element-border-color);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);

  &::placeholder {
    color: var(--pico-form-element-placeholder-color);
  }

  &:focus {
    outline: none;
    border-color: var(--pico-primary-border);
    box-shadow: 0 0 0 2px var(--pico-primary-focus);
  }
}

.btn-create {
  background: var(--pico-primary-background);
  color: var(--pico-primary-inverse);
  cursor: pointer;
  font-weight: 500;
  transition: background 0.2s;

  &:hover:not(:disabled) {
    background: var(--pico-primary-hover);
  }
}
</style>
