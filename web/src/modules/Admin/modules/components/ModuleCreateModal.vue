<script setup lang="ts">
import { ref } from 'vue'
import { createModule } from '../fetch/adminModules.fetch'
import type { AdminModule } from '../types/adminModules.types'
import toast from '@/services/toast'

const emit = defineEmits<{
  created: [module: AdminModule]
  cancel: []
}>()

const form = ref({ name: '', code: '', description: '' })
const saving = ref(false)
const error = ref<string | null>(null)

const submit = async () => {
  error.value = null
  if (!form.value.name.trim() || !form.value.code.trim()) {
    error.value = 'Le nom et le code sont obligatoires'
    return
  }
  saving.value = true
  try {
    const created = await createModule(form.value)
    toast.success('Module créé (inactif par défaut)')
    emit('created', created)
  } catch {
    error.value = 'Erreur lors de la création du module'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Nouveau module</h3>
        <p class="modal-sub">Le module sera créé inactif. Activez-le ensuite depuis la vue de modification.</p>

        <label>
          Nom <span class="required">*</span>
          <input v-model="form.name" type="text" placeholder="Nom du module" />
        </label>
        <label>
          Code <span class="required">*</span>
          <input v-model="form.code" type="text" placeholder="CODE_MODULE" />
        </label>
        <label>
          Description
          <textarea v-model="form.description" placeholder="Description…" rows="3" />
        </label>

        <p v-if="error" class="form-error">{{ error }}</p>

        <div class="modal-actions">
          <button class="btn-secondary" @click="emit('cancel')">Annuler</button>
          <button class="btn-primary" :disabled="saving" @click="submit">
            {{ saving ? 'Création…' : 'Créer' }}
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
.modal-sub { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

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
  &:hover { border-color: var(--pico-color); color: var(--pico-color); }
}

.required { color: #ef4444; }
.form-error { font-size: 0.8rem; color: #ef4444; margin: 0; }
</style>
