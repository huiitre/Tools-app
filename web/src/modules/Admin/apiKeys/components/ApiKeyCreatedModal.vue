<script setup lang="ts">
import { ref } from 'vue'
import type { CreatedApiKey } from '../types/adminApiKeys.types'
import toast from '@/services/toast'

const props = defineProps<{ result: CreatedApiKey }>()

const emit = defineEmits<{ close: [] }>()

const copied = ref(false)

const copy = async () => {
  try {
    await navigator.clipboard.writeText(props.result.key)
    copied.value = true
    toast.success('Clé copiée')
  } catch {
    toast.error('Échec de la copie')
  }
}
</script>

<template>
  <Teleport to="body">
    <!-- Pas de fermeture au clic sur le fond : un clic de travers perdrait la clé pour toujours. -->
    <div class="modal-overlay">
      <div class="modal" role="dialog" aria-modal="true" aria-label="Clé créée">
        <div class="modal-title-group">
          <i class="mdi mdi-check-circle-outline" aria-hidden="true" />
          <span class="modal-title">Clé « {{ result.apiKey.name }} » créée</span>
        </div>

        <div class="key-field">
          <code>{{ result.key }}</code>
          <button type="button" class="copy-btn" :class="{ copied }" title="Copier la clé" @click="copy">
            <i class="mdi" :class="copied ? 'mdi-check' : 'mdi-content-copy'" />
          </button>
        </div>

        <p class="warning">
          <i class="mdi mdi-alert-outline" aria-hidden="true" />
          <span>
            C'est la <strong>seule et unique fois</strong> que cette clé s'affiche en clair. Une fois
            cette fenêtre fermée, elle ne pourra plus jamais être relue : il faudra en créer une nouvelle.
          </span>
        </p>

        <div class="modal-actions">
          <button type="button" class="btn-close" @click="emit('close')">
            {{ copied ? 'Fermer' : 'Fermer sans copier' }}
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
  padding: 1.25rem;
  width: 460px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-title-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;

  i { font-size: 1.1rem; color: #22c55e; }
}

.modal-title { font-size: 0.95rem; font-weight: 700; }

.key-field {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.4rem 0.4rem 0.65rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-background-color);

  code {
    flex: 1;
    min-width: 0;
    padding: 0;
    background: none;
    font-size: 0.78rem;
    word-break: break-all;
    user-select: all;
  }
}

.copy-btn {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  margin: 0;
  padding: 0;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  i { font-size: 1rem; }
  &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
  &.copied { border-color: #22c55e; color: #22c55e; }
}

.warning {
  display: flex;
  gap: 0.5rem;
  margin: 0;
  padding: 0.55rem 0.65rem;
  border-radius: var(--pico-border-radius);
  background: color-mix(in srgb, #f59e0b 12%, transparent);
  color: #d97706;
  font-size: 0.78rem;
  line-height: 1.45;

  i { font-size: 1rem; flex-shrink: 0; }
}

.modal-actions { display: flex; justify-content: flex-end; }

.btn-close {
  width: auto;
  margin: 0;
  padding: 0.45rem 1rem;
  border: 1px solid var(--pico-form-element-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);
  font-size: 0.85rem;
  cursor: pointer;
  &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
}
</style>
