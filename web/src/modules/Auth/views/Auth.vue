<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/modules/Auth/auth.store'
import PasswordInput from '@/modules/Auth/components/PasswordInput.vue'

import websiteImg from '@/assets/img/Core/website_img.jpg'
import toast from '@/services/toast'

import {
  useFetchLogin,
  useFetchMe,
  useFetchLoginWithGoogle,
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

/* Google OAuth — ANCIEN BOUTON */
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID_WEB
const googleBtnRef = ref<HTMLElement | null>(null)

const renderGoogleButton = async () => {

  while (!window.google?.accounts?.id) {
    await new Promise(r => setTimeout(r, 50))
  }

  if (!googleBtnRef.value) return

  window.google.accounts.id.initialize({
    client_id: GOOGLE_CLIENT_ID,
    callback: async (response: any) => {
      try {
        isConnecting.value = true

        const idToken = response?.credential
        if (!idToken) throw new Error('GOOGLE_ID_TOKEN_MISSING')

        const { data } = await useFetchLoginWithGoogle({ idToken })
        auth.setToken(data.accessToken)

        const me = await useFetchMe()
        auth.setUser(me.data)

        toast.success('Connexion Google réussie')
        router.push('/')
      } catch (error: any) {
        toast.error(error?.message || 'Erreur lors de la connexion Google')
      } finally {
        isConnecting.value = false
      }
    }
  })

  googleBtnRef.value.innerHTML = ''

  window.google.accounts.id.renderButton(googleBtnRef.value, {
    theme: 'outline',
    size: 'large',
    shape: 'rectangular',
    width: 360
  })
}

onMounted(renderGoogleButton)

watch(isRegister, async (value) => {
  if (!value) {
    await nextTick()
    renderGoogleButton()
  }
})

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
        <div ref="googleBtnRef"></div>
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

.forgot-password-link {
  text-align: center;
  margin-top: 0.5rem;
}
</style>
