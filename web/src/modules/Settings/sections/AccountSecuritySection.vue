<script setup lang="ts">
import { ref } from 'vue'
import PasswordInput from '@/modules/Auth/components/PasswordInput.vue'
import {
  validatePassword,
  PasswordValidationError
} from '@/modules/Auth/views/passwordValidation'
import { useFetchSetPassword } from '@/modules/Auth/fetch/auth.fetch'

const newPassword = ref('')
const confirmPassword = ref('')
const isSubmitting = ref(false)

const feedback = ref<{
  type: 'success' | 'error'
  message: string
} | null>(null)

const onUpdatePassword = async () => {
  feedback.value = null

  try {
    isSubmitting.value = true
    validatePassword(newPassword.value, confirmPassword.value)

    await useFetchSetPassword(newPassword.value)

    feedback.value = {
      type: 'success',
      message: 'Mot de passe mis à jour avec succès.'
    }

    newPassword.value = ''
    confirmPassword.value = ''
  } catch (e) {
    if (e instanceof PasswordValidationError) {
      feedback.value = { type: 'error', message: e.message }
    } else {
      feedback.value = { type: 'error', message: 'Erreur inattendue.' }
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="account-security">
    <header class="section-header">
      <h3>Sécurité</h3>
      <p class="section-subtitle">
        Mot de passe, email et sécurité du compte.
      </p>
    </header>

    <div class="card">
      <h2 class="card-title">Changer le mot de passe</h2>

      <form @submit.prevent="onUpdatePassword">
        <div class="field">
          <label>
            Nouveau mot de passe
            <PasswordInput
              v-model="newPassword"
              autocomplete="new-password"
              placeholder="Nouveau mot de passe"
            />
          </label>
        </div>

        <div class="field">
          <label>
            Confirmer le mot de passe
            <PasswordInput
              v-model="confirmPassword"
              autocomplete="new-password"
              placeholder="Confirmation"
            />
          </label>
        </div>

        <small class="hint">
          Minimum 8 caractères, avec au moins une lettre et un chiffre.
        </small>

        <div class="actions">
          <button type="submit" :aria-busy="isSubmitting">Mettre à jour</button>
        </div>

        <p v-if="feedback" class="feedback">
          <mark v-if="feedback.type === 'success'">
            {{ feedback.message }}
          </mark>
          <mark v-else class="error">
            {{ feedback.message }}
          </mark>
        </p>
      </form>
    </div>
  </section>
</template>

<style scoped>
/* ===== Header ===== */

.section-header {
  margin-bottom: 1rem;
}

.section-subtitle {
  margin: 0.25rem 0 0;
  opacity: 0.75;
  font-size: 0.9rem;
}

/* ===== Card ===== */

.card {
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 10px;
  padding: 1rem;
  max-width: 640px;
}

.card-title {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
}

/* ===== Form ===== */

.field {
  max-width: 360px;
}

.field + .field {
  margin-top: 0.75rem;
}

.hint {
  display: block;
  margin-top: 0.5rem;
  opacity: 0.7;
  font-size: 0.85rem;
}

.actions {
  margin-top: 1rem;
}

.actions button {
  width: auto;
}

/* ===== Feedback ===== */

.feedback {
  margin-top: 0.75rem;
}

.feedback .error {
  background: #ffe3e3;
}
</style>