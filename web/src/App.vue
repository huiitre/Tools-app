<script setup lang="ts">
import Page from '@/router/Page.vue'
import { onMounted, onBeforeUnmount } from 'vue'
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

//* Un 403 peut signifier deux choses très différentes : une action interdite sur une page à
//* laquelle on a droit, ou un module qui vient d'être retiré. Seul le profil relu permet de
//* trancher — d'où cet aller-retour avant toute redirection.
let checkingAccess = false

const onAccessForbidden = async () => {
  //* Une page qui échoue lance souvent plusieurs requêtes d'un coup : sans ce verrou, chaque
  //* 403 relancerait son propre /me.
  if (checkingAccess) return
  checkingAccess = true

  try {
    await authStore.refreshUser()

    //* Les droits n'ont pas bougé : le 403 visait l'action, pas le module. L'écran affiche
    //* déjà son erreur, il n'y a rien à faire de plus.
    if (!isModuleAllowed(route)) {
      toast.error("Vous n'avez plus accès à ce module.")
      await router.push('/')
    }
  } catch {
    //* Le profil est resté celui qu'on connaissait : mieux vaut ne rien faire que rediriger
    //* sur une lecture qui a échoué pour une autre raison.
  } finally {
    checkingAccess = false
  }
}

onMounted(() => {
  window.addEventListener('auth:expired', onAuthExpired)
  window.addEventListener('access:forbidden', onAccessForbidden)
})

onBeforeUnmount(() => {
  window.removeEventListener('auth:expired', onAuthExpired)
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
