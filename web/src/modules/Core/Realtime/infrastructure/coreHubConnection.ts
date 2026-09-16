import * as signalR from '@microsoft/signalr'
import { CORE_BASE_URL, refreshSession } from '@/services/axiosInstance'
import { useAuthStore } from '@/modules/Auth/auth.store'

const log = (...args: unknown[]) => console.log('[Realtime]', ...args)

// Connexion SignalR unique vers CoreHub (voir CoreHub côté API) : tout module qui a besoin d'un
// event temps réel (notifications, changement de rôle…) s'abonne ici plutôt que d'ouvrir sa
// propre HubConnection — un deuxième WebSocket vers le même hub doublerait l'auth, la logique
// de reconnexion et le throttle de renouvellement de jeton ci-dessous, pour rien.
// Même cadence que la politique de reconnexion de SignalR ci-dessous : un seul délai à retenir.
const INITIAL_CONNECT_RETRY_MS = 5000

class CoreHubConnection {
  private connection: signalR.HubConnection | null = null
  private lastTokenRefresh = 0
  // `withAutomaticReconnect` ne couvre que « connecté puis perdu » : le tout premier `start()`
  // n'a pas encore atteint l'état Connected, donc jamais de transition vers Reconnecting à
  // observer. Sans cette boucle manuelle, un premier essai malchanceux (hub indisponible pile au
  // chargement de la page) laissait le hub mort pour le reste de la session — rien ne rappelait
  // `start()` ensuite.
  private retryTimer: ReturnType<typeof setTimeout> | null = null
  private stopped = false

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
        // Le niveau par défaut (Information) logue l'URL complète de chaque WebSocket ouvert,
        // token d'accès inclus (il voyage en query string, seul moyen sur une poignée de main
        // WebSocket) — visible en prod, dans la console de n'importe qui. Warning tait ce
        // message sans perdre les vraies erreurs de la lib.
        .configureLogging(signalR.LogLevel.Warning)
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
    this.stopped = false
    const connection = this.ensureBuilt()

    connection.onreconnected(() => { log('Hub reconnecté'); onConnect?.() })
    connection.onreconnecting(() => { log('Hub en reconnexion'); onError?.() })
    connection.onclose(() => { log('Hub fermé'); onError?.() })

    this.attemptStart(connection, onConnect, onError)
  }

  private attemptStart(connection: signalR.HubConnection, onConnect?: () => void, onError?: () => void): void {
    connection.start().then(() => onConnect?.()).catch(() => {
      onError?.()
      if (this.stopped) return

      log(`Connexion initiale au hub échouée, nouvel essai dans ${INITIAL_CONNECT_RETRY_MS / 1000}s.`)
      this.retryTimer = setTimeout(() => this.attemptStart(connection, onConnect, onError), INITIAL_CONNECT_RETRY_MS)
    })
  }

  disconnect(): void {
    this.stopped = true
    if (this.retryTimer) {
      clearTimeout(this.retryTimer)
      this.retryTimer = null
    }
    this.connection?.stop()
    this.connection = null
  }
}

export const coreHubConnection = new CoreHubConnection()
