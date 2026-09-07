<script setup lang="ts">
import { onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import toast from '@/services/toast'
import { useFetchVerifyEmail } from '@/modules/Auth/fetch/auth.fetch'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { clientInit } from '@/services/axiosInstance'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  const token = route.query.token as string | undefined

  if (!token) {
    router.back()
    return
  }

  try {
    // La réponse porte `status` : confirmer son adresse n'ouvre pas toujours la connexion.
    const { data } = await useFetchVerifyEmail(token)

    if (data?.status === 'PENDING_APPROVAL') {
      toast.success('Adresse confirmée. Votre compte doit être activé par un administrateur.')
    } else {
      toast.success('Adresse email confirmée, vous pouvez vous connecter.')
    }
  } catch (error: any) {
    toast.error(error?.message || 'Lien de validation invalide ou expiré.')
  }

  // Le lien de confirmation concerne le compte qui vient de s'inscrire, jamais celui qui était
  // déjà connecté dans ce navigateur. Sans cette purge, le `beforeEach` retrouvait la session
  // précédente et renvoyait sur l'accueil : l'écran affichait un compte connecté juste après
  // avoir annoncé qu'il fallait attendre une activation.
  if (auth.user || auth.accessToken) {
    try {
      await clientInit.post('/auth/logout')
    } catch {
      // Le cookie disparaîtra de toute façon à son expiration : rien à signaler ici.
    }

    auth.logout()
  }

  // Laisse le flush DOM du montage courant (et du toast) se terminer avant de
  // déclencher la transition de route : sinon <Transition mode="out-in"> peut
  // enchaîner sur le composant async suivant avant que ce montage soit committé.
  await nextTick()
  router.push('/login')
})
</script>

<template>
  <main class="verify-email">
    <p>Validation de votre adresse email en cours…</p>
  </main>
</template>

<style scoped>
.verify-email {
  width: 100%;
  padding: 2rem 1rem;
  display: flex;
  justify-content: center;
  font-size: 1rem;
  opacity: 0.7;
}
</style>
