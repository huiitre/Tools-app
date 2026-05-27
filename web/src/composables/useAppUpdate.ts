import { ref, onMounted, onUnmounted } from 'vue'
import { updateService } from '@/services/update/update.service'
import { useEnv } from './useEnv'

export type UpdateState = 'idle' | 'available' | 'downloading' | 'ready'

const { isElectron, isWeb } = useEnv()
const updateState = ref<UpdateState>('idle')
const downloadProgress = ref(0)

updateService.onUpdateAvailable(() => {
  // Web : pas d'étape de téléchargement, on passe directement à ready
  updateState.value = isElectron ? 'available' : 'ready'
})

updateService.onDownloadProgress((percent: number) => {
  downloadProgress.value = percent
})

updateService.onUpdateReady(() => {
  updateState.value = 'ready'
})

export function useAppUpdate() {
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