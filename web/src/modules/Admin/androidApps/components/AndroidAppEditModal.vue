<script setup lang="ts">
import { ref } from 'vue'
import { updateAndroidApp } from '../fetch/adminAndroidApps.fetch'
import { ANDROID_APP_LABEL_MAX_LENGTH, type AndroidApp } from '../types/adminAndroidApps.types'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

const props = defineProps<{ app: AndroidApp }>()
const emit = defineEmits<{
  updated: [app: AndroidApp]
  cancel: []
}>()

const label = ref(props.app.label)
const displayOrder = ref(props.app.displayOrder)
const saving = ref(false)
const error = ref<string | null>(null)

const save = async () => {
  error.value = null
  const trimmed = label.value.trim()

  if (!trimmed || trimmed.length > ANDROID_APP_LABEL_MAX_LENGTH) {
    error.value = `Le nom doit faire entre 1 et ${ANDROID_APP_LABEL_MAX_LENGTH} caractères`
    return
  }

  saving.value = true
  try {
    const app = await updateAndroidApp(props.app.id, { label: trimmed, displayOrder: Number(displayOrder.value) })
    toast.success('App mise à jour')
    emit('updated', app)
  } catch (e) {
    error.value = e instanceof ApiException ? e.message : "Erreur lors de la mise à jour de l'app"
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="!saving && emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Modifier l'app</h3>
        <p class="modal-sub mono">{{ app.packageName }}</p>

        <label>
          Nom affiché sur les appareils
          <input v-model="label" type="text" :maxlength="ANDROID_APP_LABEL_MAX_LENGTH" :disabled="saving" @keyup.enter="save" />
        </label>
        <label>
          Ordre d'affichage
          <input v-model.number="displayOrder" type="number" :disabled="saving" @keyup.enter="save" />
        </label>

        <p v-if="error" class="form-error">{{ error }}</p>

        <div class="modal-actions">
          <button class="btn-secondary" :disabled="saving" @click="emit('cancel')">Annuler</button>
          <button class="btn-primary" :disabled="saving" :aria-busy="saving" @click="save">
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
  input { margin: 0; font-size: 0.85rem; }
}

.modal-title { font-size: 1rem; font-weight: 700; margin: 0; }
.modal-sub { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }
.mono { font-family: var(--pico-font-family-monospace); }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }
.form-error { font-size: 0.8rem; color: #ef4444; margin: 0; }

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
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-color); color: var(--pico-color); }
}
</style>
