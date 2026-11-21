<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { updateAndroidApp } from '../fetch/adminAndroidApps.fetch'
import { ANDROID_APP_ADMIN_NOTES_MAX_LENGTH, type AndroidApp } from '../types/adminAndroidApps.types'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

// Notes libres sur une app (où télécharger ses APK, dépôt GitHub…) : visibles seulement ici, jamais
// sur les appareils. Un clic à côté annule.
const props = defineProps<{ app: AndroidApp }>()

const emit = defineEmits<{
  updated: [app: AndroidApp]
  cancel: []
}>()

const notes = ref(props.app.adminNotes ?? '')
const saving = ref(false)
const error = ref<string | null>(null)
const textarea = ref<HTMLTextAreaElement | null>(null)

onMounted(() => textarea.value?.focus())

const cancel = () => {
  if (!saving.value) emit('cancel')
}

const save = async () => {
  error.value = null
  saving.value = true

  try {
    // Une chaîne vide efface les notes côté API.
    const app = await updateAndroidApp(props.app.id, { adminNotes: notes.value })
    toast.success('Notes enregistrées')
    emit('updated', app)
  } catch (e) {
    error.value = e instanceof ApiException ? e.message : "Erreur lors de l'enregistrement des notes"
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="overlay" @click.self="cancel">
      <div class="popup">
        <header class="popup-header">
          <div class="identity">
            <h3>Notes · {{ app.label }}</h3>
            <span class="package">{{ app.packageName }}</span>
          </div>
          <button type="button" class="icon-btn" title="Fermer" :disabled="saving" @click="cancel">
            <i class="mdi mdi-close" />
          </button>
        </header>

        <div class="popup-body">
          <textarea
            ref="textarea"
            v-model="notes"
            class="field"
            rows="8"
            :maxlength="ANDROID_APP_ADMIN_NOTES_MAX_LENGTH"
            :disabled="saving"
            placeholder="Où trouver les APK de cette app : URL du dépôt GitHub, page de téléchargement…"
            @keydown.ctrl.enter="save"
          />
          <p class="hint">
            Visible seulement dans l'administration. Ctrl + Entrée pour enregistrer.
            <span class="count">{{ notes.length }} / {{ ANDROID_APP_ADMIN_NOTES_MAX_LENGTH }}</span>
          </p>
        </div>

        <footer class="popup-footer">
          <p v-if="error" class="error">{{ error }}</p>
          <button type="button" class="btn-ghost" :disabled="saving" @click="cancel">Annuler</button>
          <button type="button" class="btn-primary" :disabled="saving" :aria-busy="saving" @click="save">
            <i v-if="!saving" class="mdi mdi-content-save-outline" />
            Enregistrer
          </button>
        </footer>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9000;
}

.popup {
  display: flex;
  flex-direction: column;
  width: 560px;
  max-width: calc(100vw - 2rem);
  max-height: calc(100vh - 3rem);
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, .35);
}

.popup-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 1rem 1.1rem 0.6rem;
}

.identity {
  display: flex;
  flex-direction: column;
  min-width: 0;

  h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--pico-contrast);
  }
}

.package {
  font-family: var(--pico-font-family-monospace);
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.popup-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 0 1.1rem 0.75rem;
}

.field {
  width: 100%;
  margin: 0;
  padding: 0.6rem 0.7rem;
  font-size: 0.85rem;
  line-height: 1.5;
  border-radius: 8px;
  resize: vertical;
}

.hint {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin: 0.4rem 0 0;
  font-size: 0.72rem;
  color: var(--pico-muted-color);
}

.popup-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 0.75rem 1.1rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);
}

.error {
  flex: 1;
  margin: 0;
  font-size: 0.78rem;
  color: #e53e3e;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
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

  &:hover:not(:disabled) {
    background: color-mix(in srgb, var(--pico-contrast) 10%, transparent);
    color: var(--pico-contrast);
  }
}

.btn-primary,
.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  margin: 0;
  padding: 0.4rem 1rem;
  font-size: 0.82rem;
  border-radius: 8px;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  color: inherit;

  &:hover:not(:disabled) {
    border-color: var(--pico-contrast);
  }
}
</style>
