<script setup lang="ts">
import Page from '@/router/Page.vue'
import { onMounted, onBeforeUnmount, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import toast from '@/services/toast'
import { clientInit } from './services/axiosInstance'
import FullPageLoader from './components/ui/FullPageLoader.vue'
import { useUIStore } from '@/stores/ui.store'
import ImagePreviewModal from '@/components/ui/ImagePreviewModal.vue'
import ValorantSkinDetailModal from '@/modules/Riot/valorant/components/ValorantSkinDetailModal.vue'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { isModuleAllowed } from '@/router/moduleAccess'

const uiStore = useUIStore()
const authStore = useAuthStore()

const route = useRoute()
const router = useRouter()

const onAuthExpired = async () => {
  uiStore.setLoading(true)

  try {
    await clientInit.post('/auth/logout')
  } catch {
    // ignore
  }

  toast.error('Votre session a expiré. Veuillez vous reconnecter.')

  if (route.meta.requireAuth === true) {
    await router.push('/login')
  }

  uiStore.setLoading(false)
}

//* Les droits ont changé pendant qu'on est sur une page : reste-t-elle permise ?
//*
//* `router.beforeEach` ne s'exécute qu'à une navigation, et l'intercepteur 403 ne voit que ce
//* qui part sur le réseau. Une page déjà chargée qui n'appelle plus rien — l'Atelier Dofus, par
//* exemple — échappait donc aux deux : ses onglets se grisaient bien, puisqu'ils observent les
//* droits, mais personne ne rejouait le contrôle de la route elle-même.
//*
//* Observer `user` plutôt qu'un event dédié couvre du même coup l'accès module révoqué, le
//* module désactivé globalement et le rôle abaissé : tout ce qui passe par `refreshUser()`, sans
//* que l'API ait à publier quoi que ce soit de particulier.
watch(
  () => authStore.user,
  () => {
    if (!authStore.user) return

    if (!isModuleAllowed(route)) {
      toast.error("Vous n'avez plus accès à ce module.")
      router.push('/')
    }
  },
)

//* Le compte vient d'être désactivé par un administrateur, annoncé par le hub. Le déroulé est
//* celui d'une session expirée — purge du cookie, retour au login —, seul le message change :
//* dire « votre session a expiré » enverrait la personne se reconnecter pour rien.
const onAccountDeactivated = async () => {
  uiStore.setLoading(true)

  try {
    await clientInit.post('/auth/logout')
  } catch {
    // ignore
  }

  //* Vidé après l'appel : logout n'a pas besoin du jeton, mais les stores de session le sont.
  authStore.logout()

  toast.error('Votre compte a été désactivé par un administrateur.')

  await router.push('/login')

  uiStore.setLoading(false)
}

//* Un 403 peut signifier deux choses très différentes : une action interdite sur une page à
//* laquelle on a droit, ou un module qui vient d'être retiré. Ce handler ne tranche pas — il
//* relit le profil, et c'est le `watch` ci-dessus qui redirige si les droits ont réellement
//* disparu. Décider ici aussi produirait deux toasts et deux redirections pour un seul refus.
//*
//* Il reste utile malgré l'event temps réel : le hub peut être déconnecté, ou le changement
//* venir d'ailleurs que d'une révocation d'accès.
let checkingAccess = false

const onAccessForbidden = async () => {
  //* Une page qui échoue lance souvent plusieurs requêtes d'un coup : sans ce verrou, chaque
  //* 403 relancerait son propre /me.
  if (checkingAccess) return
  checkingAccess = true

  try {
    await authStore.refreshUser()
  } catch {
    //* Le profil est resté celui qu'on connaissait. L'écran affiche déjà son erreur, et on ne
    //* redirige personne sur une lecture qui a pu échouer pour une tout autre raison.
  } finally {
    checkingAccess = false
  }
}

onMounted(() => {
  window.addEventListener('auth:expired', onAuthExpired)
  window.addEventListener('auth:deactivated', onAccountDeactivated)
  window.addEventListener('access:forbidden', onAccessForbidden)
})

onBeforeUnmount(() => {
  window.removeEventListener('auth:expired', onAuthExpired)
  window.removeEventListener('auth:deactivated', onAccountDeactivated)
  window.removeEventListener('access:forbidden', onAccessForbidden)
})
</script>

<template>
  <router-view v-slot="{ Component }">
    <Page>
      <Transition name="page" mode="out-in">
        <component :is="Component" />
      </Transition>
    </Page>
  </router-view>

  <FullPageLoader :visible="uiStore.isLoading" />
  <ImagePreviewModal />
  <ValorantSkinDetailModal />
</template>

<style lang="scss" scoped>
.page-enter-active,
.page-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
