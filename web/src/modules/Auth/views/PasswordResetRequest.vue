<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import toast from '@/services/toast'
import { useFetchPasswordResetRequest } from '@/modules/Auth/fetch/auth.fetch'
import { useAuthStore } from '@/modules/Auth/auth.store'

const router = useRouter()
const authStore = useAuthStore()

onMounted(() => {
  if (authStore.isAuthenticated) {
    if (window.history.length > 1) router.back()
    else router.replace('/')
  }
})

const email = ref('')
const isSubmitting = ref(false)

const onSubmit = async () => {
  try {
    isSubmitting.value = true

    const { data } = await useFetchPasswordResetRequest({
      email: email.value
    })

    toast.success(
      data?.message ??
        'Si un compte existe avec cette adresse email, un lien de réinitialisation a été envoyé.',
      5000
    )

    router.push('/login')

  } catch (error: any) {
    toast.error(
      error?.message || 'Erreur lors de la demande de réinitialisation'
    )
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="password-reset-request">

    <hgroup class="reset-title">
      <h1>Mot de passe oublié</h1>
      <p>
        Entrez votre adresse email pour recevoir un lien de réinitialisation.
      </p>
    </hgroup>

    <form @submit.prevent="onSubmit">
      <input
        v-model="email"
        type="email"
        name="email"
        autocomplete="email"
        placeholder="Adresse email"
        required
      />

      <button :aria-busy="isSubmitting" type="submit">
        Envoyer le lien
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

.password-reset-request {
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
