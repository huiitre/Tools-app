import { ref, onMounted, onUnmounted } from 'vue'
import { updateService } from '@/services/update/update.service'
import { useEnv } from './useEnv'

const updateAvailable = ref(false)

updateService.onUpdateAvailable(() => {
  console.log('[PWA] updateAvailable = true')
  updateAvailable.value = true
})

export function useAppUpdate() {
  const { isWeb } = useEnv()

  onMounted(() => {
    if (!isWeb) return
    const interval = setInterval(() => {
      console.log('[PWA] Vérification mise à jour...')
      navigator.serviceWorker.getRegistration().then(r => r?.update())
    }, 10 * 1000)

    onUnmounted(() => clearInterval(interval))
  })

  return {
    updateAvailable,
    applyUpdate: () => updateService.applyUpdate(),
  }
}