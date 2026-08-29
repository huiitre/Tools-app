<script setup lang="ts">
import { ref } from 'vue'
import { updateModule } from '../fetch/adminModules.fetch'
import type { AdminModule } from '../types/adminModules.types'
import toast from '@/services/toast'

const props = defineProps<{ module: AdminModule }>()
const emit = defineEmits<{
  updated: [module: AdminModule]
  cancel: []
}>()

const form = ref({ ...props.module })
const saving = ref(false)

const save = async () => {
  saving.value = true
  try {
    await updateModule(props.module.id, form.value)
    toast.success('Module mis à jour')
    emit('updated', { ...form.value })
  } catch {
    toast.error('Erreur lors de la mise à jour du module')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Modifier le module</h3>

        <label>
          Nom
          <input v-model="form.name" type="text" placeholder="Nom du module" />
        </label>
        <label>
          Code
          <input v-model="form.code" type="text" placeholder="CODE_MODULE" />
        </label>
        <label>
          Description
          <textarea v-model="form.description" placeholder="Description…" rows="3" />
        </label>
        <label class="toggle-row">
          <span>Actif</span>
          <input type="checkbox" role="switch" v-model="form.active" />
        </label>

        <div class="modal-actions">
          <button class="btn-secondary" @click="emit('cancel')">Annuler</button>
          <button class="btn-primary" :disabled="saving" @click="save">
            {{ saving ? 'Enregistrement…' : 'Enregistrer' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.65rem;
  padding: 1.5rem;
  width: 420px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);

  label { display: flex; flex-direction: column; gap: 0.3rem; font-size: 0.82rem; font-weight: 500; margin: 0; }
  input, textarea { margin: 0; font-size: 0.85rem; }
}

.modal-title { font-size: 1rem; font-weight: 700; margin: 0; }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

.toggle-row {
  flex-direction: row !important;
  align-items: center;
  justify-content: space-between;
  input { margin: 0; }
}

.btn-primary {
  padding: 0.45rem 1rem;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { opacity: 0.85; }
}

.btn-secondary {
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:hover { border-color: inherit; color: inherit; }
}
</style>
