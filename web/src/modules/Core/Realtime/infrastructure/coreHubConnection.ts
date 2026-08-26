import * as signalR from '@microsoft/signalr'
import { CORE_BASE_URL, refreshSession } from '@/services/axiosInstance'
import { useAuthStore } from '@/modules/Auth/auth.store'

const log = (...args: unknown[]) => console.log('[Realtime]', ...args)

// Connexion SignalR unique vers CoreHub (voir CoreHub côté API) : tout module qui a besoin d'un
// event temps réel (notifications, changement de rôle…) s'abonne ici plutôt que d'ouvrir sa
// propre HubConnection — un deuxième WebSocket vers le même hub doublerait l'auth, la logique
// de reconnexion et le throttle de renouvellement de jeton ci-dessous, pour rien.
class CoreHubConnection {
  private connection: signalR.HubConnection | null = null
  private lastTokenRefresh = 0

  // Le hub est le seul appel du front qui ne passe pas par l'intercepteur axios : personne
  // d'autre ne renouvelle son jeton sur un 401. Au plus un refresh par minute, pour qu'une API
  // indisponible ne déclenche pas un refresh à chaque tentative de reconnexion (toutes les 5s).
  private async accessToken(): Promise<string> {
    const auth = useAuthStore()
    if (Date.now() - this.lastTokenRefresh > 60_000) {
      this.lastTokenRefresh = Date.now()
      try {
        await refreshSession()
      } catch {
        // Session irrécupérable : on présente le jeton courant, le hub retentera.
      }
    }
    return auth.accessToken ?? ''
  }

  // Construit la connexion sans la démarrer, pour qu'un `on()` appelé avant `connect()`
  // enregistre bien son handler avant que le hub ne commence à recevoir des messages.
  private ensureBuilt(): signalR.HubConnection {
    if (!this.connection) {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${CORE_BASE_URL}/hub`, { accessTokenFactory: () => this.accessToken() })
        // La politique par défaut abandonne après quatre essais (0, 2, 10 et 30s), soit
        // quarante secondes — moins que le redémarrage d'un conteneur. Ici on retente
        // indéfiniment, toutes les cinq secondes.
        .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 5000 })
        .build()
    }
    return this.connection
  }

  on<T = unknown>(eventType: string, handler: (payload: T) => void): void {
    this.ensureBuilt().on(eventType, handler)
  }

  off(eventType: string): void {
    this.connection?.off(eventType)
  }

  connect(onConnect?: () => void, onError?: () => void): void {
    const connection = this.ensureBuilt()

    connection.onreconnected(() => { log('Hub reconnecté'); onConnect?.() })
    connection.onreconnecting(() => { log('Hub en reconnexion'); onError?.() })
    connection.onclose(() => { log('Hub fermé'); onError?.() })

    connection.start().then(() => onConnect?.()).catch(() => onError?.())
  }

  disconnect(): void {
    this.connection?.stop()
    this.connection = null
  }
}

export const coreHubConnection = new CoreHubConnection()
