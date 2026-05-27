import { ref, onMounted, onUnmounted } from 'vue'
import { updateService } from '@/services/update/update.service'
import { useEnv } from './useEnv'

const updateReady = ref(false)

updateService.onUpdateReady(() => {
  updateReady.value = true
})

export function useAppUpdate() {
  const { isWeb } = useEnv()

  onMounted(() => {
    if (!isWeb) return
    const interval = setInterval(() => {
      navigator.serviceWorker.getRegistration().then(r => r?.update())
    }, 10 * 1000)
    onUnmounted(() => clearInterval(interval))
  })

  return {
    updateReady,
    applyUpdate: () => updateService.applyUpdate(),
  }
}