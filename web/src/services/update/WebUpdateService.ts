import { pwa } from "@/services/update/pwa"
import type { IUpdateService } from '@/services/update/IUpdateService'

export class WebUpdateService implements IUpdateService {
  onUpdateAvailable(callback: () => void): void {
    pwa.onNeedRefresh = callback
  }

  applyUpdate(): void {
    // 1. Écouteur global pour le changement de contrôleur (plus fiable que statechange)
    navigator.serviceWorker.addEventListener('controllerchange', () => {
      window.location.reload()
    }, { once: true })

    navigator.serviceWorker.getRegistration().then(reg => {
      if (reg?.waiting) {
        // 2. On envoie l'ordre de prendre le contrôle
        reg.waiting.postMessage({ type: 'SKIP_WAITING' })
      } else {
        // Fallback si pas de worker en attente
        window.location.reload()
      }
    })

    // 3. Sécurité : force le reload si l'événement n'est pas reçu après 2s
    setTimeout(() => {
      window.location.reload()
    }, 2000)
  }
}
