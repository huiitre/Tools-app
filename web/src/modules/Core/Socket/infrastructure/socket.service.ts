import { Client, IFrame, IMessage, StompSubscription } from '@stomp/stompjs';

export interface SocketSubscriptionRequest {
  destination: string;
  callback: (message: IMessage) => void;
}

export class SocketService {
  private static instance: SocketService;
  private client: Client | null = null;
  private isConnected = false;
  private currentToken: string | null = null;
  // On garde une trace permanente des abonnements souhaités pour les restaurer au reconnect
  private subscriptionsRegistry: Map<string, (message: IMessage) => void> = new Map();
  private activeSubscriptions: Map<string, StompSubscription> = new Map();

  private constructor() {}

  public static getInstance(): SocketService {
    if (!SocketService.instance) {
      SocketService.instance = new SocketService();
    }
    return SocketService.instance;
  }

  public connect(url: string, token?: string, onConnect?: () => void, onError?: (frame: IFrame) => void): void {
    const newToken = token || null;

    // Si on est déjà connecté avec le même token, on ne fait rien
    if (this.client && this.isConnected && this.currentToken === newToken) {
      if (onConnect) onConnect();
      return;
    }

    // Si le token a changé, on repart de zéro
    if (this.currentToken !== newToken && this.client) {
      this.disconnect();
    }

    if (this.client && this.client.active) {
        return;
    }

    this.currentToken = newToken;

    this.client = new Client({
      brokerURL: url,
      connectHeaders: token ? { Authorization: `Bearer ${token}` } : {},
      debug: (str) => {
        if (import.meta.env.DEV) console.log('[STOMP]', str);
      },
      reconnectDelay: 5000, // Reconnexion auto toutes les 5s
      heartbeatIncoming: 4000,
      heartbeatOutgoing: 4000,
    });

    this.client.onConnect = (frame) => {
      this.isConnected = true;
      console.log('[STOMP] Connected');
      
      // On vide les anciens objets de souscription (ils ne sont plus valides)
      this.activeSubscriptions.clear();

      // On restaure TOUS les abonnements enregistrés dans le registre
      this.subscriptionsRegistry.forEach((callback, destination) => {
        console.log(`[STOMP] Restoring subscription to ${destination}`);
        const sub = this.client!.subscribe(destination, callback);
        this.activeSubscriptions.set(destination, sub);
      });
      
      if (onConnect) onConnect();
    };

    this.client.onStompError = (frame) => {
      console.error('[STOMP] Error', frame.headers['message']);
      if (onError) onError(frame);
    };

    this.client.onWebSocketClose = () => {
      if (this.isConnected) {
        this.isConnected = false;
        console.log('[STOMP] WebSocket Closed');
        if (onError) onError({} as IFrame);
      }
    };

    this.client.activate();
  }

  public subscribe(destination: string, callback: (message: IMessage) => void): StompSubscription | null {
    // On enregistre dans le registre permanent
    this.subscriptionsRegistry.set(destination, callback);

    if (!this.client || !this.isConnected) {
      return null;
    }

    // Si déjà abonné, on nettoie l'ancien objet
    this.unsubscribe(destination, false); // false = ne pas supprimer du registre

    const subscription = this.client.subscribe(destination, callback);
    this.activeSubscriptions.set(destination, subscription);
    return subscription;
  }

  public unsubscribe(destination: string, removeFromRegistry = true): void {
    if (removeFromRegistry) {
      this.subscriptionsRegistry.delete(destination);
    }

    const subscription = this.activeSubscriptions.get(destination);
    if (subscription) {
      try {
        if (this.client && this.isConnected) {
          subscription.unsubscribe();
        }
      } catch (e) {}
      this.activeSubscriptions.delete(destination);
    }
  }

  public disconnect(): void {
    if (this.client) {
      this.client.deactivate();
      this.client = null;
      this.isConnected = false;
      this.activeSubscriptions.clear();
      this.subscriptionsRegistry.clear();
      this.currentToken = null;
    }
  }

  public send(destination: string, body: any): void {
    if (this.client && this.isConnected) {
      this.client.publish({
        destination,
        body: JSON.stringify(body),
      });
    }
  }

  public get active(): boolean {
    return this.isConnected;
  }
}

export const socketService = SocketService.getInstance();
