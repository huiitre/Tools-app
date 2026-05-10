<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useNotificationStore } from '@/modules/Core/Notification/store/notification.store'

const store = useNotificationStore()
const open = ref(false)
const popoverRef = ref<HTMLElement | null>(null)

const ICON_MAP = {
  SUCCESS: 'mdi-check-circle-outline',
  INFO: 'mdi-information-outline',
  WARNING: 'mdi-alert-outline',
  ERROR: 'mdi-alert-circle-outline',
}

function formatRelative(dateStr: string): string {
  const date = new Date(dateStr)
  const diff = Math.floor((Date.now() - date.getTime()) / 1000)
  if (diff < 60) return 'À l\'instant'
  if (diff < 3600) return `il y a ${Math.floor(diff / 60)} min`
  if (diff < 86400) return `il y a ${Math.floor(diff / 3600)}h`
  const days = Math.floor(diff / 86400)
  return `il y a ${days} jour${days > 1 ? 's' : ''}`
}

const toggle = () => { open.value = !open.value }

const close = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (!target.closest('.notif-wrapper')) open.value = false
}

const onScroll = (e: Event) => {
  if (open.value && !popoverRef.value?.contains(e.target as Node)) open.value = false
}

onMounted(() => {
  document.addEventListener('click', close)
  window.addEventListener('scroll', onScroll, true)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', close)
  window.removeEventListener('scroll', onScroll, true)
})
</script>

<template>
  <div class="notif-wrapper">
    <button
      class="notif-trigger"
      aria-label="Notifications"
      :title="store.isConnected ? 'Notifications (Connecté)' : 'Notifications (Déconnecté)'"
      @click="toggle"
    >
      <i class="mdi mdi-bell-outline" aria-hidden="true"></i>
      <span v-if="store.hasUnread" class="notif-badge">
        {{ store.unreadCount > 9 ? '9+' : store.unreadCount }}
      </span>
      <div v-if="!store.isConnected" class="notif-conn-dot" title="Erreur de connexion SSE"></div>
    </button>

    <Transition name="notif-popover">
      <div v-if="open" ref="popoverRef" class="notif-popover" role="dialog" aria-label="Notifications">
        <div class="notif-header">
          <span class="notif-header-title">Notifications</span>
          <div class="notif-header-actions">
            <button
              v-if="store.hasUnread"
              class="notif-icon-btn"
              aria-label="Tout marquer comme lu"
              title="Tout marquer comme lu"
              @click.stop="store.markAsRead()"
            >
              <i class="mdi mdi-check-all" aria-hidden="true"></i>
            </button>
            <button
              v-if="store.notifications.length > 0"
              class="notif-icon-btn notif-icon-btn--danger"
              aria-label="Tout supprimer"
              title="Tout supprimer"
              @click.stop="store.remove()"
            >
              <i class="mdi mdi-trash-can-outline" aria-hidden="true"></i>
            </button>
          </div>
        </div>

        <ul class="notif-list" role="list">
          <li
            v-for="notif in store.notifications"
            :key="notif.id"
            class="notif-item"
            :class="[`notif-item--${notif.type}`, { 'notif-item--unread': !notif.read }]"
            @mouseenter="!notif.read && store.markAsRead([notif.id])"
          >
            <div v-if="!notif.read" class="notif-item-dot" aria-label="Non lu"></div>
            <div class="notif-item-icon">
              <i class="mdi" :class="ICON_MAP[notif.type]" aria-hidden="true"></i>
            </div>
            <div class="notif-item-content">
              <p class="notif-item-title">{{ notif.title }}</p>
              <p class="notif-item-body" style="white-space: pre-wrap;">{{ notif.body }}</p>
              <div class="notif-item-footer">
                <span class="notif-item-time">{{ formatRelative(notif.createdAt) }}</span>
                <button
                  class="notif-item-delete"
                  aria-label="Supprimer"
                  title="Supprimer"
                  @click.stop="store.remove([notif.id])"
                >
                  <i class="mdi mdi-delete-outline" aria-hidden="true"></i>
                </button>
              </div>
            </div>
          </li>
        </ul>

        <div v-if="store.notifications.length === 0" class="notif-empty">
          <i class="mdi mdi-bell-off-outline" aria-hidden="true"></i>
          <p>Aucune notification</p>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.notif-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
}

/* Bouton déclencheur */
.notif-trigger {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  height: 2.25rem;
  padding: 0 0.5rem;

  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  cursor: pointer;
  color: inherit;

  &:hover { background: var(--pico-muted-background-color); }
  .mdi { font-size: 1.25rem; }
}

/* Badge compteur */
.notif-badge {
  position: absolute;
  top: -5px;
  right: -5px;

  min-width: 17px;
  height: 17px;
  padding: 0 3px;

  display: flex;
  align-items: center;
  justify-content: center;

  border-radius: 999px;
  background: var(--pico-del-color, #c0392b);
  color: #fff;
  font-size: 0.6rem;
  font-weight: 700;
  line-height: 1;
  pointer-events: none;
}

/* Point statut connexion */
.notif-conn-dot {
  position: absolute;
  bottom: 2px;
  left: 2px;
  width: 6px;
  height: 6px;
  background: var(--pico-del-color);
  border-radius: 50%;
}

/* Popover */
.notif-popover {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  z-index: 1100;

  width: 380px;
  max-height: 480px;
  overflow: hidden;
  display: flex;
  flex-direction: column;

  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);
}

/* Transition */
.notif-popover-enter-active,
.notif-popover-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.notif-popover-enter-from,
.notif-popover-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.98);
}

/* En-tête popover */
.notif-header {
  display: flex;
  align-items: center;
  justify-content: space-between;

  padding: 0.65rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-shrink: 0;
}

.notif-header-title {
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--pico-muted-color);
}

.notif-header-actions {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

/* Boutons icône dans le header */
.notif-icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;

  width: 1.75rem;
  height: 1.75rem;
  border-radius: var(--pico-border-radius);
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--pico-muted-color);
  transition: background 0.12s ease, color 0.12s ease;

  .mdi { font-size: 1.1rem; }

  &:hover {
    background: var(--pico-muted-background-color);
    color: var(--pico-color);
  }
}

.notif-icon-btn--danger:hover {
  background: color-mix(in srgb, var(--pico-del-color, #c0392b) 12%, transparent);
  color: var(--pico-del-color, #c0392b);
}

/* Liste */
.notif-list {
  overflow-y: auto;
  flex: 1;
  margin: 0;
  padding: 0;
  list-style: none;
}

/* Item */
.notif-item {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;

  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  cursor: default;
  transition: background 0.12s ease;

  &:last-child { border-bottom: none; }
  &:hover { background: var(--pico-muted-background-color); }
}

.notif-item--unread {
  background: color-mix(in srgb, var(--pico-primary) 5%, transparent);

  &:hover { background: color-mix(in srgb, var(--pico-primary) 10%, transparent); }
}

/* Icône colorée selon type */
.notif-item-icon {
  flex-shrink: 0;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 0.1rem;

  .mdi { font-size: 1.1rem; }
}

.notif-item--SUCCESS .notif-item-icon {
  background: color-mix(in srgb, #2ecc71 15%, transparent);
  color: #27ae60;
}
.notif-item--INFO .notif-item-icon {
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
}
.notif-item--WARNING .notif-item-icon {
  background: color-mix(in srgb, #f39c12 15%, transparent);
  color: #d68910;
}
.notif-item--ERROR .notif-item-icon {
  background: color-mix(in srgb, #e74c3c 15%, transparent);
  color: #c0392b;
}

/* Contenu */
.notif-item-content {
  flex: 1;
  min-width: 0;
}

.notif-item-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--pico-color);
  margin: 0 0 0.2rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.notif-item-body {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  margin: 0 0 0.35rem;
  line-height: 1.4;
  white-space: pre-wrap;
  word-break: break-word;
}

/* Footer : date + poubelle sur la même ligne */
.notif-item-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.notif-item-time {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  opacity: 0.7;
}

.notif-item-delete {
  display: flex;
  align-items: center;
  justify-content: center;

  width: 1.3rem;
  height: 1.3rem;
  border-radius: var(--pico-border-radius);
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--pico-muted-color);
  opacity: 0;
  transition: opacity 0.12s ease, background 0.12s ease, color 0.12s ease;

  .mdi { font-size: 0.85rem; }
}

.notif-item:hover .notif-item-delete {
  opacity: 1;
}

.notif-item-delete:hover {
  background: color-mix(in srgb, var(--pico-del-color, #c0392b) 12%, transparent);
  color: var(--pico-del-color, #c0392b);
}

/* Point non-lu — positionné en absolu, ne pousse rien */
.notif-item-dot {
  position: absolute;
  right: 0.75rem;
  transform: translateY(-50%);
  top: 15px;

  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--pico-primary);
  pointer-events: none;
}

/* État vide */
.notif-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2.5rem 1rem;
  color: var(--pico-muted-color);

  .mdi { font-size: 2rem; }
  p { margin: 0; font-size: 0.85rem; }
}
</style>
