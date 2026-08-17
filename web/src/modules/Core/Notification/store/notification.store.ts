import { defineStore } from 'pinia';
import { computed, ref, watch } from 'vue';
import { AppNotification } from '../domain/notification.types';
import { SignalRNotificationTransport } from '../infrastructure/notification.transport';
import { clientCore, CORE_BASE_URL } from '@/services/axiosInstance';
import { useAuthStore } from '@/modules/Auth/auth.store';

const log = (...args: unknown[]) => console.log('[Realtime]', ...args);

export const useNotificationStore = defineStore('notifications', () => {
  const notifications = ref<AppNotification[]>([]);
  const isConnected = ref(false);
  const transport = new SignalRNotificationTransport();
  const authStore = useAuthStore();

  let connectedWithToken: string | null = null;
  let pendingReconnect = false;

  const unreadCount = computed(() => notifications.value.filter(n => !n.read).length);
  const hasUnread = computed(() => unreadCount.value > 0);

  async function fetchHistory() {
    const { data } = await clientCore.get<AppNotification[]>('/notifications');
    notifications.value = data;
  }

  async function init() {
    if (!authStore.isAuthenticated || isConnected.value) return;

    try {
      await fetchHistory();

      const token = authStore.accessToken;

      if (token) {
        connectedWithToken = token;
        const hubUrl = `${CORE_BASE_URL}/hub`;
        log('Connexion au hub temps réel...');
        transport.connect(
          hubUrl,
          token,
          () => {
            isConnected.value = true;
            log('Hub connecté');
            if (pendingReconnect) {
              pendingReconnect = false;
              fetchHistory().catch(() => {});
            }
          },
          (newNotif) => handleIncoming(newNotif),
          () => {
            isConnected.value = false;
            log('Hub en erreur / déconnecté');
            if (!authStore.isAuthenticated) {
              log('Non authentifié → abandon');
              transport.disconnect();
              connectedWithToken = null;
              return;
            }

            // SignalR (withAutomaticReconnect) retente seul. On se contente de marquer
            // qu'un rafraîchissement de l'historique sera nécessaire au retour.
            pendingReconnect = true;
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
      await clientCore.patch('/notifications/read', null, { params });

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
      await clientCore.delete('/notifications', { params });

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
