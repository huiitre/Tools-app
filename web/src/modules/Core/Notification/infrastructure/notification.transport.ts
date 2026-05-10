import { AppNotification } from '../domain/notification.types';

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
      // On NE ferme PAS le flux ici pour laisser EventSource tenter la reconnexion automatique
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
