<script setup lang="ts">
import { ref } from 'vue'
import { createVpnPeer } from '../fetch/adminVpn.fetch'
import { VPN_PEER_NAME_PATTERN } from '../types/adminVpn.types'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

const emit = defineEmits<{
  created: []
  cancel: []
}>()

const name = ref('')
const saving = ref(false)
const error = ref<string | null>(null)

const submit = async () => {
  error.value = null
  const value = name.value.trim()

  if (!value) {
    error.value = 'Le nom est obligatoire'
    return
  }
  if (!VPN_PEER_NAME_PATTERN.test(value)) {
    error.value = 'Lettres, chiffres, tiret ou underscore, 31 caractères max, et un caractère alphanumérique en tête'
    return
  }

  saving.value = true
  try {
    await createVpnPeer(value)
    toast.success(`Peer « ${value} » créé`)
    emit('created')
  } catch (e) {
    // Un nom déjà pris ou refusé est un cas métier : le message de l'API est plus utile que le nôtre.
    error.value = e instanceof ApiException ? e.message : 'Erreur lors de la création du peer'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Nouveau peer</h3>
        <p class="modal-sub">
          Les clés et l'adresse sont générées par le serveur WireGuard. Le peer est actif dès sa création.
        </p>

        <label>
          Nom
          <input
            v-model="name"
            type="text"
            placeholder="portable-julie"
            autofocus
            :disabled="saving"
            @keyup.enter="submit"
          />
        </label>

        <p v-if="error" class="form-error">{{ error }}</p>

        <div class="modal-actions">
          <button class="btn-secondary" :disabled="saving" @click="emit('cancel')">Annuler</button>
          <button class="btn-create" :disabled="saving" :aria-busy="saving" @click="submit">
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
  input { height: 2rem; margin: 0; font-size: 0.75rem; }
}

.modal-title { font-size: 1rem; font-weight: 700; margin: 0; }
.modal-sub { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

.btn-create {
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

.form-error { font-size: 0.8rem; color: #ef4444; margin: 0; }
</style>
