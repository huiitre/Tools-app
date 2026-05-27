import { ref, onMounted, onUnmounted } from 'vue'
import { updateService } from '@/services/update/update.service'
import { useEnv } from './useEnv'

const updateState = ref<'idle' | 'available' | 'downloading' | 'ready'>('idle')
const downloadProgress = ref(0)

updateService.onUpdateAvailable(() => {
  updateState.value = 'available'
})

updateService.onDownloadProgress((percent) => {
  downloadProgress.value = percent
})

updateService.onUpdateReady(() => {
  updateState.value = 'ready'
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

  function startDownload() {
    updateState.value = 'downloading'
    downloadProgress.value = 0
    updateService.startDownload()
  }

  return {
    updateState,
    downloadProgress,
    startDownload,
    applyUpdate: () => updateService.applyUpdate(),
  }
}
