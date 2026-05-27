<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/modules/Auth/auth.store'
import PasswordInput from '@/modules/Auth/components/PasswordInput.vue'

import websiteImg from '@/assets/img/Core/website_img.jpg'
import toast from '@/services/toast'

import {
  useFetchLogin,
  useFetchMe,
  useFetchGoogleAuthUrl,
  useFetchRegister
} from '../fetch/auth.fetch'

import { validatePassword, PasswordValidationError } from '@/modules/Auth/views/passwordValidation'
import { useEnv } from '@/composables/useEnv'

const { isElectron } = useEnv()

const auth = useAuthStore()
const router = useRouter()

const isRegister = ref(false)

/* Connexion */
const loginEmail = ref('')
const loginPassword = ref('')
const isConnecting = ref(false)

/* Inscription */
const registerEmail = ref('')
const registerUsername = ref('')
const registerPassword = ref('')
const registerConfirmPassword = ref('')
const isSubmittingRegister = ref(false)

/* Google OAuth */
const isGoogleLoading = ref(false)

const loginWithGoogle = async () => {
  try {
    isGoogleLoading.value = true
    const { data } = await useFetchGoogleAuthUrl('web')
    window.location.href = data.url
  } catch {
    toast.error('Erreur lors de la connexion Google')
    isGoogleLoading.value = false
  }
}

/* Email / Password */
const onLoginSubmit = async () => {
  try {
    isConnecting.value = true

    const { data } = await useFetchLogin({
      email: loginEmail.value,
      password: loginPassword.value
    })

    auth.setToken(data.accessToken)

    const me = await useFetchMe()
    auth.setUser(me.data)

    toast.success('Connexion réussie')
    router.push('/')
  } catch (error: any) {
    if (error instanceof PasswordValidationError) {
      toast.warning(error.message)
      return
    }
    toast.error(error?.message || 'Erreur lors de la connexion')
  } finally {
    isConnecting.value = false
  }
}

const onRegisterSubmit = async () => {
  try {
    isSubmittingRegister.value = true
    validatePassword(registerPassword.value, registerConfirmPassword.value)

    const { data } = await useFetchRegister({
      name: registerUsername.value,
      email: registerEmail.value,
      password: registerPassword.value
    })

    toast.success(data.message || 'Compte créé avec succès')

    isRegister.value = false
    loginEmail.value = registerEmail.value
    loginPassword.value = registerPassword.value
    clearRegisterFields()
  } catch (error: any) {
    if (error instanceof PasswordValidationError) {
      toast.warning(error.message)
      return
    }
    toast.error(error?.message || 'Erreur lors de la création du compte')
  } finally {
    isSubmittingRegister.value = false
  }
}

const clearRegisterFields = () => {
  registerUsername.value = ''
  registerEmail.value = ''
  registerPassword.value = ''
  registerConfirmPassword.value = ''
}
</script>

<template>
  <main class="login">
    <img class="login-logo" :src="websiteImg" alt="Logo" />

    <label class="mode-toggle">
      <input type="checkbox" role="switch" v-model="isRegister" />
      <span>{{ isRegister ? 'Inscription' : 'Connexion' }}</span>
    </label>

    <!-- CONNEXION -->
    <form v-if="!isRegister" @submit.prevent="onLoginSubmit">
      <input
        v-model="loginEmail"
        type="text"
        autocomplete="email"
        placeholder="Adresse email"
        required
      />

      <PasswordInput
        v-model="loginPassword"
        autocomplete="current-password"
        placeholder="Mot de passe"
      />

      <button :aria-busy="isConnecting" type="submit">
        Se connecter
      </button>

      <p class="forgot-password-link">
        <router-link to="/forgot-password">
          Mot de passe oublié ?
        </router-link>
      </p>
    </form>

    <!-- INSCRIPTION -->
    <form v-else @submit.prevent="onRegisterSubmit">
      <input
        v-model="registerUsername"
        type="text"
        autocomplete="username"
        placeholder="Nom d'utilisateur"
        required
      />

      <input
        v-model="registerEmail"
        type="email"
        autocomplete="email"
        placeholder="Adresse email"
        required
      />

      <PasswordInput
        v-model="registerPassword"
        autocomplete="new-password"
        placeholder="Mot de passe"
      />

      <PasswordInput
        v-model="registerConfirmPassword"
        autocomplete="new-password"
        placeholder="Confirmer le mot de passe"
      />

      <button :aria-busy="isSubmittingRegister" type="submit">
        Créer un compte
      </button>
    </form>

    <!-- OAuth -->
    <template v-if="!isRegister && !isElectron">
      <small class="separator">ou</small>

      <div class="oauth-wrapper">
        <button
          class="google-btn"
          :aria-busy="isGoogleLoading"
          @click="loginWithGoogle"
          type="button"
        >
          <svg v-if="!isGoogleLoading" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48" width="18" height="18">
            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.18 1.48-4.97 2.31-8.16 2.31-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
          </svg>
          Se connecter avec Google
        </button>
      </div>
    </template>
  </main>
</template>

<style scoped>
.login {
  width: 100%;
  padding: 1.5rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

.login-logo {
  width: 140px;
  height: 140px;
  border-radius: 50%;
}

form {
  width: 100%;
  max-width: 360px;
}

.mode-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.separator {
  opacity: 0.6;
}

.oauth-wrapper {
  display: flex;
  justify-content: center;
  width: 100%;
}

.google-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  width: 100%;
  max-width: 360px;
}

.forgot-password-link {
  text-align: center;
  margin-top: 0.5rem;
}
</style>
