<script setup lang="ts">
import { computed, ref } from 'vue'
import ApiKeyScopeSelectorModal from './ApiKeyScopeSelectorModal.vue'
import { createApiKey } from '../fetch/adminApiKeys.fetch'
import {
  API_KEY_DESCRIPTION_MAX_LENGTH,
  API_KEY_MAX_LIFETIME_YEARS,
  API_KEY_NAME_MAX_LENGTH,
  type CreatedApiKey,
} from '../types/adminApiKeys.types'
import { ApiException } from '@/services/ApiException'

const props = defineProps<{ scopes: string[] }>()

const emit = defineEmits<{
  created: [result: CreatedApiKey]
  cancel: []
}>()

// Date du calendrier local au format attendu par <input type="date">.
const toInputDate = (date: Date) => {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

const addDays = (days: number) => { const d = new Date(); d.setDate(d.getDate() + days); return d }
const addYears = (years: number) => { const d = new Date(); d.setFullYear(d.getFullYear() + years); return d }

const minDate = toInputDate(addDays(1))
const maxDate = toInputDate(addYears(API_KEY_MAX_LIFETIME_YEARS))

const PRESETS = [
  { label: '30 jours', date: () => toInputDate(addDays(30)) },
  { label: '90 jours', date: () => toInputDate(addDays(90)) },
  { label: '1 an', date: () => toInputDate(addYears(1)) },
  { label: '3 ans', date: () => maxDate },
]

const name = ref('')
const description = ref('')
const selectedScopes = ref<string[]>([])
const expiresOn = ref(toInputDate(addYears(1)))
const scopeSelectorOpen = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)

const nameValid = computed(() => {
  const value = name.value.trim()
  return value.length > 0 && value.length <= API_KEY_NAME_MAX_LENGTH
})

const descriptionValid = computed(() => {
  const value = description.value.trim()
  return value.length > 0 && value.length <= API_KEY_DESCRIPTION_MAX_LENGTH
})

const expirationValid = computed(() => !!expiresOn.value && expiresOn.value >= minDate && expiresOn.value <= maxDate)

const canSubmit = computed(() =>
  nameValid.value && descriptionValid.value && selectedScopes.value.length > 0 && expirationValid.value && !saving.value
)

// Valable jusqu'à la fin du jour choisi, sans dépasser les 3 ans comptés depuis maintenant :
// la création arrive quelques instants après, l'API refuserait la toute dernière minute.
const expiresAtIso = () => {
  const [y, m, d] = expiresOn.value.split('-').map(Number)
  const endOfDay = new Date(y, m - 1, d, 23, 59, 59)
  const limit = addYears(API_KEY_MAX_LIFETIME_YEARS)
  limit.setMinutes(limit.getMinutes() - 1)
  return (endOfDay < limit ? endOfDay : limit).toISOString()
}

const removeScope = (scope: string) => {
  selectedScopes.value = selectedScopes.value.filter(s => s !== scope)
}

const applyScopes = (scopes: string[]) => {
  selectedScopes.value = scopes
  scopeSelectorOpen.value = false
}

const submit = async () => {
  if (!canSubmit.value) return
  error.value = null
  saving.value = true
  try {
    const result = await createApiKey({
      name: name.value.trim(),
      description: description.value.trim(),
      expiresAt: expiresAtIso(),
      scopes: selectedScopes.value,
    })
    emit('created', result)
  } catch (e) {
    // Un nom déjà pris ou une date refusée est un cas métier : le message de l'API est le bon.
    error.value = e instanceof ApiException ? e.message : 'Erreur lors de la création de la clé'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Nouvelle clé d'API</h3>
        <p class="modal-sub">
          Pour un service du NAS qui appelle l'API sans compte utilisateur. La clé n'ouvre que les scopes choisis.
        </p>

        <label>
          <span class="label-row">Nom <small>{{ name.trim().length }}/{{ API_KEY_NAME_MAX_LENGTH }}</small></span>
          <input
            v-model="name"
            type="text"
            placeholder="Extracteur Temtem"
            :maxlength="API_KEY_NAME_MAX_LENGTH"
            autofocus
            :disabled="saving"
          />
        </label>

        <label>
          <span class="label-row">Description <small>{{ description.trim().length }}/{{ API_KEY_DESCRIPTION_MAX_LENGTH }}</small></span>
          <textarea
            v-model="description"
            rows="3"
            placeholder="Synchronisation du catalogue Temtem depuis le NAS"
            :maxlength="API_KEY_DESCRIPTION_MAX_LENGTH"
            :disabled="saving"
          />
        </label>

        <div class="field">
          <span class="label-row">Scopes <small>{{ selectedScopes.length }} sélectionné{{ selectedScopes.length > 1 ? 's' : '' }}</small></span>
          <div class="scope-field">
            <button type="button" class="pill-btn" :disabled="saving" @click="scopeSelectorOpen = true">
              <i class="mdi mdi-tune-variant" />{{ selectedScopes.length ? 'Modifier les scopes' : 'Choisir les scopes' }}
            </button>
            <button
              v-for="scope in selectedScopes"
              :key="scope"
              type="button"
              class="scope-chip"
              :title="`Retirer ${scope}`"
              :disabled="saving"
              @click="removeScope(scope)"
            >
              {{ scope }}<i class="mdi mdi-close" />
            </button>
          </div>
        </div>

        <div class="field">
          <span class="label-row">Expiration <small>{{ API_KEY_MAX_LIFETIME_YEARS }} ans maximum</small></span>
          <div class="expiration-field">
            <input v-model="expiresOn" type="date" :min="minDate" :max="maxDate" :disabled="saving" />
            <button
              v-for="preset in PRESETS"
              :key="preset.label"
              type="button"
              class="pill-btn"
              :class="{ active: expiresOn === preset.date() }"
              :disabled="saving"
              @click="expiresOn = preset.date()"
            >
              {{ preset.label }}
            </button>
          </div>
          <small class="hint">Valable jusqu'à la fin du jour choisi.</small>
        </div>

        <p v-if="error" class="form-error">{{ error }}</p>

        <div class="modal-actions">
          <button class="btn-secondary" :disabled="saving" @click="emit('cancel')">Annuler</button>
          <button class="btn-create" :disabled="!canSubmit" :aria-busy="saving" @click="submit">
            {{ saving ? 'Création…' : 'Créer' }}
          </button>
        </div>
      </div>
    </div>

    <ApiKeyScopeSelectorModal
      :open="scopeSelectorOpen"
      :scopes="props.scopes"
      :model-value="selectedScopes"
      @apply="applyScopes"
      @close="scopeSelectorOpen = false"
    />
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
  width: 520px;
  max-width: 90vw;
  max-height: calc(100vh - 2rem);
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);

  label, .field { display: flex; flex-direction: column; gap: 0.3rem; font-size: 0.82rem; font-weight: 500; margin: 0; }
  input { height: 2rem; margin: 0; font-size: 0.75rem; }
  textarea { margin: 0; font-size: 0.75rem; resize: vertical; }
}

.label-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;

  small { color: var(--pico-muted-color); font-size: 0.7rem; font-weight: 400; }
}

.modal-title { font-size: 1rem; font-weight: 700; margin: 0; }
.modal-sub { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }
.hint { color: var(--pico-muted-color); font-size: 0.7rem; font-weight: 400; }

.scope-field,
.expiration-field {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
}

.expiration-field input { width: auto; }

.pill-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  margin: 0;
  padding: 0.3rem 0.75rem;
  border: 1px solid var(--pico-card-border-color);
  border-radius: 999px;
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.75rem;
  cursor: pointer;

  &:hover:not(:disabled), &.active { border-color: var(--pico-primary); color: var(--pico-primary); }
  &.active { font-weight: 700; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.scope-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  width: auto;
  margin: 0;
  padding: 0.2rem 0.55rem;
  border: 1px solid transparent;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
  color: var(--pico-primary);
  font-family: monospace;
  font-size: 0.72rem;
  cursor: pointer;

  i { font-size: 0.75rem; opacity: 0.7; }
  &:hover:not(:disabled) { border-color: #ef4444; color: #ef4444; background: color-mix(in srgb, #ef4444 10%, transparent); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

.btn-create {
  width: auto;
  margin: 0;
  padding: 0.45rem 1rem;
  border: 1px solid var(--pico-form-element-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-primary); color: var(--pico-primary); }
}

.btn-secondary {
  width: auto;
  margin: 0;
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-muted-color); color: inherit; }
}

.form-error { font-size: 0.8rem; color: #ef4444; margin: 0; }
</style>
