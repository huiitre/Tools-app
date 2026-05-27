import { pwa } from "@/services/update/pwa"
import type { IUpdateService } from '@/services/update/IUpdateService'

export class WebUpdateService implements IUpdateService {
  onUpdateAvailable(_callback: () => void): void {
    // Web: le SW télécharge automatiquement, onUpdateReady se déclenche directement
  }

  onDownloadProgress(_callback: (percent: number) => void): void {
    // Web: pas de suivi de progression (géré par le SW)
  }

  onUpdateReady(callback: () => void): void {
    pwa.onNeedRefresh = callback
  }

  startDownload(): void {
    // Web: no-op, le SW gère le téléchargement
  }

  applyUpdate(): void {
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      window.location.reload()
    }, { once: true })

    navigator.serviceWorker.getRegistration().then(reg => {
      if (reg?.waiting) {
        reg.waiting.postMessage({ type: 'SKIP_WAITING' })
      } else {
        window.location.reload()
      }
    })

    setTimeout(() => {
      window.location.reload()
    }, 2000)
  }
}
