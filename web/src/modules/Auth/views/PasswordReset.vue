<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import toast from '@/services/toast'
import { validatePassword, PasswordValidationError } from '@/modules/Auth/views/passwordValidation'
import { useFetchPasswordReset } from '../fetch/auth.fetch'
import PasswordInput from '@/modules/Auth/components/PasswordInput.vue'

const props = defineProps<{
  token: string
}>()

const router = useRouter()

const password = ref('')
const confirmPassword = ref('')
const isSubmitting = ref(false)

const onSubmit = async () => {
  try {
    isSubmitting.value = true

    validatePassword(password.value, confirmPassword.value)

    const { data } = await useFetchPasswordReset({
      token: props.token,
      password: password.value
    })

    toast.success(
      data?.message ?? 'Votre mot de passe a été réinitialisé avec succès.',
      5000
    )

    router.push('/login')

  } catch (error: any) {
    if (error instanceof PasswordValidationError) {
      toast.error(error.message)
      return
    }

    toast.error(
      error?.message || 'Erreur lors de la réinitialisation du mot de passe'
    )
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  if (!props.token) {
    if (window.history.length > 1) router.back()
    else router.replace('/')
  }
})
</script>

<template>
  <main class="password-reset">
    <hgroup class="reset-title">
      <h1>Nouveau mot de passe</h1>
      <p>
        Choisissez un nouveau mot de passe pour votre compte.
      </p>
    </hgroup>

    <form @submit.prevent="onSubmit">
      <PasswordInput
        v-model="password"
        name="password"
        autocomplete="new-password"
        placeholder="Nouveau mot de passe"
      />

      <PasswordInput
        v-model="confirmPassword"
        name="confirmPassword"
        autocomplete="new-password"
        placeholder="Confirmer le mot de passe"
      />

      <button :aria-busy="isSubmitting" type="submit">
        Réinitialiser le mot de passe
      </button>
    </form>

    <p class="back-to-login">
      <router-link to="/login">
        Retour à la connexion
      </router-link>
    </p>
  </main>
</template>

<style scoped>
.reset-title {
  text-align: center;
}

.reset-title h1 {
  margin-bottom: 0.5rem;
}

.password-reset {
  width: 100%;
  padding: 1.5rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

form {
  width: 100%;
  max-width: 360px;
}

.back-to-login {
  text-align: center;
}
</style>
