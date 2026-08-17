import * as signalR from '@microsoft/signalr';
import { AppNotification } from '../domain/notification.types';
import { socketService } from '../../Socket/infrastructure/socket.service';

export interface NotificationTransport {
  connect(
    url: string, 
    token: string, 
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
    token: string, 
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    if (this.eventSource) this.disconnect();

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
    token: string, 
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    // On transforme l'URL API en URL WebSocket
    // url ressemble à "http://localhost:8083/api/v3/ws"
    const wsUrl = url.replace('http', 'ws'); 

    socketService.connect(
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
    );
  }

  disconnect(): void {
    // On déconnecte tout le socket lors d'un logout pour nettoyer la session backend
    socketService.disconnect();
  }
}

// Point de connexion réel de l'API Core (voir CoreHub) : notifications, et demain tout autre
// événement temps réel, sur la même connexion.
export class SignalRNotificationTransport implements NotificationTransport {
  private connection: signalR.HubConnection | null = null;

  connect(
    url: string,
    token: string,
    onConnect: () => void,
    onMessage: (notif: AppNotification) => void,
    onError: () => void
  ): void {
    if (this.connection) this.disconnect();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveNotification', onMessage);
    this.connection.onreconnected(() => onConnect());
    this.connection.onreconnecting(() => onError());
    this.connection.onclose(() => onError());

    this.connection.start().then(onConnect).catch(() => onError());
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
  }
}
