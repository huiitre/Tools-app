<script setup lang="ts">
import Page from '@/router/Page.vue'
import { onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import toast from '@/services/toast'
import { clientInit } from './services/axiosInstance'
import FullPageLoader from './components/ui/FullPageLoader.vue'
import { useUIStore } from '@/stores/ui.store'
import ImagePreviewModal from '@/components/ui/ImagePreviewModal.vue'

const uiStore = useUIStore()

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

onMounted(() => {
  window.addEventListener('auth:expired', onAuthExpired)
})

onBeforeUnmount(() => {
  window.removeEventListener('auth:expired', onAuthExpired)
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
