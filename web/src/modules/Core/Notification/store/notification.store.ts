import { defineStore } from 'pinia';
import { computed, ref, watch } from 'vue';
import { AppNotification } from '../domain/notification.types';
import { SseNotificationTransport } from '../infrastructure/notification.transport';
import { clientV3 } from '@/services/axiosInstance';
import { useAuthStore } from '@/modules/Auth/auth.store';

const log = (...args: unknown[]) => console.log('[SSE]', ...args);

export const useNotificationStore = defineStore('notifications', () => {
  const notifications = ref<AppNotification[]>([]);
  const isConnected = ref(false);
  const transport = new SseNotificationTransport();
  const authStore = useAuthStore();

  let connectedWithToken: string | null = null;
  let pendingReconnect = false;

  const unreadCount = computed(() => notifications.value.filter(n => !n.read).length);
  const hasUnread = computed(() => unreadCount.value > 0);

  async function fetchHistory() {
    const { data } = await clientV3.get<AppNotification[]>('/notifications');
    notifications.value = data;
  }

  async function init() {
    if (!authStore.isAuthenticated || isConnected.value) return;

    try {
      await fetchHistory();

      const baseUrl = import.meta.env.VITE_TOOLS_API_BASE_URL || '';
      const token = authStore.accessToken;

      if (token) {
        connectedWithToken = token;
        const streamUrl = `${baseUrl}/api/v3/notifications/stream`;
        log('Connexion au stream...');
        transport.connect(
          streamUrl,
          token,
          () => {
            isConnected.value = true;
            log('Stream connecté');
            if (pendingReconnect) {
              pendingReconnect = false;
              fetchHistory().catch(() => {});
            }
          },
          (newNotif) => handleIncoming(newNotif),
          () => {
            isConnected.value = false;
            log('Stream erreur / déconnecté');
            if (!authStore.isAuthenticated) {
              log('Non authentifié → abandon');
              transport.disconnect();
              connectedWithToken = null;
              return;
            }
            const currentToken = authStore.accessToken;
            if (currentToken && currentToken !== connectedWithToken) {
              log('Nouveau token détecté → reconnexion directe');
              transport.disconnect();
              connectedWithToken = null;
              pendingReconnect = false;
              init();
            } else {
              log('Tentative de rafraîchissement / reconnexion...');
              transport.disconnect();
              connectedWithToken = null;
              
              // On attend 3s avant de retenter (pour laisser le serveur rebooter si besoin)
              setTimeout(() => {
                fetchHistory()
                  .then(() => {
                    log('Serveur OK → reconnexion au stream');
                    init();
                  })
                  .catch((err) => {
                    // Si c'est une 401, le rafraîchissement a probablement déjà échoué via l'intercepteur Axios
                    // Sinon (ex: CONNECTION_REFUSED), on retente plus tard
                    if (err?.response?.status !== 401) {
                      log('Serveur injoignable, nouvelle tentative dans 10s...');
                      setTimeout(() => init(), 10000);
                    }
                  });
              }, 3000);
            }
          }
        );
      }
    } catch (e) {
      // Échec silencieux
    }
  }

  function handleIncoming(notif: AppNotification) {
    if (!notifications.value.some(n => n.id === notif.id)) {
      notifications.value.unshift(notif);

      if (document.hidden && window.Notification && window.Notification.permission === 'granted') {
        new window.Notification(notif.title, { body: notif.body });
      }
    }
  }

  async function markAsRead(ids?: number[]) {
    try {
      const params = ids ? { ids: ids.join(',') } : {};
      await clientV3.patch('/notifications/read', null, { params });

      if (!ids) {
        notifications.value.forEach(n => n.read = true);
      } else {
        notifications.value.forEach(n => {
          if (ids.includes(n.id)) n.read = true;
        });
      }
    } catch (e) {
      // Erreur silencieuse
    }
  }

  async function remove(ids?: number[]) {
    try {
      const params = ids ? { ids: ids.join(',') } : {};
      await clientV3.delete('/notifications', { params });

      if (!ids) {
        notifications.value = [];
      } else {
        notifications.value = notifications.value.filter(n => !ids.includes(n.id));
      }
    } catch (e) {
      // Erreur silencieuse
    }
  }

  function disconnect() {
    log('Déconnexion volontaire');
    transport.disconnect();
    isConnected.value = false;
    notifications.value = [];
    connectedWithToken = null;
    pendingReconnect = false;
  }

  watch(() => authStore.isAuthenticated, (val) => {
    if (val) init();
    else disconnect();
  }, { immediate: true });

  return {
    notifications,
    unreadCount,
    hasUnread,
    isConnected,
    markAsRead,
    remove,
    init,
    disconnect
  };
});
