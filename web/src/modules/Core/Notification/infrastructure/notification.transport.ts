import { AppNotification } from '../domain/notification.types';
import { socketService } from '../../Socket/infrastructure/socket.service';
import { coreHubConnection } from '../../Realtime/infrastructure/coreHubConnection';

// `getToken` est une fonction et non une chaîne : le transport doit pouvoir redemander un jeton
// à chaque tentative de connexion. Une reconnexion survient parfois bien après l'établissement
// initial — au redémarrage de l'API, par exemple — et le jeton de départ a alors expiré depuis
// longtemps. Passer la valeur au lieu du moyen de l'obtenir condamnait toute reconnexion à un 401.
export interface NotificationTransport {
  connect(
    url: string,
    getToken: () => string | Promise<string>,
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void;
  disconnect(): void;
}

export class SseNotificationTransport implements NotificationTransport {
  private eventSource: EventSource | null = null;

  connect(
    url: string,
    getToken: () => string | Promise<string>,
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    if (this.eventSource) this.disconnect();

    Promise.resolve(getToken()).then((token) => {
      const sseUrl = `${url}?token=${token}`;
      this.eventSource = new EventSource(sseUrl);

      // Confirmation de connexion via l'événement standard open
      this.eventSource.onopen = () => {
        onConnect();
      };

      this.eventSource.addEventListener('notification', (event) => {
        try {
          const notif = JSON.parse(event.data);
          onMessage(notif);
        } catch (e) {
          // Erreur de parsing ignorée
        }
      });

      this.eventSource.onerror = () => {
        // On ferme le flux car s'il y a une erreur (ex: 401),
        // EventSource va tenter de boucler indéfiniment avec l'ancien token.
        this.disconnect();
        onError();
      };
    });
  }

  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }
}

export class WebSocketNotificationTransport implements NotificationTransport {
  connect(
    url: string,
    getToken: () => string | Promise<string>,
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    // On transforme l'URL API en URL WebSocket
    // url ressemble à "http://localhost:8083/api/v3/ws"
    const wsUrl = url.replace('http', 'ws');

    Promise.resolve(getToken()).then((token) => socketService.connect(
      wsUrl,
      token,
      () => {
        onConnect();
        // Abonnement spécifique aux notifications
        socketService.subscribe('/user/queue/core/notifications', (message) => {
          try {
            const notif = JSON.parse(message.body);
            onMessage(notif);
          } catch (e) {
            console.error('[WS] Notification parsing error', e);
          }
        });
      },
      (frame) => {
        onError();
      }
    ));
  }

  disconnect(): void {
    // On déconnecte tout le socket lors d'un logout pour nettoyer la session backend
    socketService.disconnect();
  }
}

// Point de connexion réel de l'API Core (voir CoreHub) : notifications, et tout autre événement
// temps réel (ex: changement de rôle, cf. Core/Realtime), sur la même connexion partagée —
// `coreHubConnection` la possède, ce transport ne fait que s'y abonner à son event.
export class SignalRNotificationTransport implements NotificationTransport {
  connect(
    _url: string,
    _getToken: () => string | Promise<string>,
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    coreHubConnection.on<AppNotification>('Core.ReceiveNotification', onMessage);
    coreHubConnection.connect(onConnect, onError);
  }

  disconnect(): void {
    coreHubConnection.disconnect();
  }
}
