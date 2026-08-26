<script setup lang="ts">
import { ref } from 'vue'
import type { Feedback } from '@/modules/Core/Feedback/types/feedback.types'
import { useAdminFeedbacksStore } from '../store/adminFeedbacks.store'
import { batchDeleteFeedbacks, batchUpdateFeedbacksReadStatus } from '@/modules/Core/Feedback/fetch/feedback.fetch'
import toast from '@/services/toast'

const props = defineProps<{ feedback: Feedback }>()

const store = useAdminFeedbacksStore()
const marking  = ref(false)
const deleting  = ref(false)
const showConfirm = ref(false)

const formatDate = (iso: string) => {
  try {
    return new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(iso))
  } catch { return iso }
}

const toggleRead = async () => {
  if (marking.value) return
  marking.value = true
  const next = !props.feedback.isRead
  try {
    await batchUpdateFeedbacksReadStatus([props.feedback.id], next)
    store.markReadLocally(props.feedback.id, next)
  } catch {
    toast.error('Erreur lors de la mise à jour')
  } finally {
    marking.value = false
  }
}

const confirmDelete = async () => {
  if (deleting.value) return
  deleting.value = true
  try {
    await batchDeleteFeedbacks([props.feedback.id])
    store.removeLocally(props.feedback.id)
  } catch {
    toast.error('Erreur lors de la suppression')
  } finally {
    deleting.value = false
    showConfirm.value = false
  }
}
</script>

<template>
  <div class="feedback-row" :class="{ unread: !feedback.isRead, read: feedback.isRead }" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <div class="cell cell--name">{{ feedback.userName }}</div>

    <div class="cell cell--message">{{ feedback.message }}</div>

    <div class="cell cell--date">{{ formatDate(feedback.createdAt) }}</div>

    <div class="cell cell--read">
      <button
        class="status-badge"
        :class="feedback.isRead ? 'status-badge--read' : 'status-badge--unread'"
        :disabled="marking"
        :aria-busy="marking"
        @click="toggleRead"
      >
        {{ feedback.isRead ? 'Lu' : 'Non lu' }}
      </button>
    </div>

    <div class="cell cell--actions">
      <button class="delete-btn" :disabled="deleting" title="Supprimer" @click="showConfirm = true">
        <i class="mdi mdi-delete-outline" />
      </button>
    </div>
  </div>

  <Teleport to="body">
    <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title-group">
            <i class="mdi mdi-delete-outline" aria-hidden="true"></i>
            <span class="modal-title">Supprimer ce feedback</span>
          </div>
          <button class="close-btn" :disabled="deleting" @click="showConfirm = false">
            <i class="mdi mdi-close" />
          </button>
        </div>

        <p class="modal-sub">Cette action est irréversible. Le feedback de <strong>{{ feedback.userName }}</strong> sera définitivement supprimé.</p>

        <div class="modal-footer">
          <button class="btn-secondary" :disabled="deleting" @click="showConfirm = false">Annuler</button>
          <button class="btn-danger" :disabled="deleting" :aria-busy="deleting" @click="confirmDelete">
            {{ deleting ? 'Suppression…' : 'Supprimer' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.feedback-row {
  display: grid;
  align-items: start;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.85rem;

  &:hover { box-shadow: inset 0 0 0 2px var(--pico-primary-border); }
  &.unread { border-left: 3px solid var(--pico-primary); }
  &.read { opacity: 0.55; }
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--name    { font-weight: 600; align-self: center; }
  &--message { color: var(--pico-muted-color); white-space: pre-wrap; overflow: visible; text-overflow: unset; line-height: 1.7; }
  &--date    { font-size: 0.8rem; color: var(--pico-muted-color); align-self: center; }
  &--read    { display: flex; align-items: center; align-self: center; }
  &--actions { display: flex; align-items: center; justify-content: center; align-self: center; }
}

.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
  border: none;
  cursor: pointer;
  &:disabled { opacity: 0.6; cursor: not-allowed; }

  &--read {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    color: #16a34a;
    &:hover:not(:disabled) { background: color-mix(in srgb, #22c55e 22%, transparent); }
  }
  &--unread {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
    &:hover:not(:disabled) { background: color-mix(in srgb, var(--pico-primary) 22%, transparent); }
  }
}

.delete-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  border-radius: 0.35rem;

  i { font-size: 1rem; }

  &:hover:not(:disabled) { color: #ef4444; background: color-mix(in srgb, #ef4444 10%, transparent); }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
}

/* ── Modal ── */
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.65rem;
  padding: 1.5rem;
  width: 420px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.modal-title-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  i { font-size: 0.9rem; color: #ef4444; }
}

.modal-title { font-size: 1rem; font-weight: 700; }

.modal-sub {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin: 0;
  line-height: 1.5;
}

.close-btn {
  width: 1.75rem;
  height: 1.75rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  border-radius: 0.35rem;
  &:hover:not(:disabled) { color: var(--pico-color); background: var(--pico-muted-border-color); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 0.25rem;
}

.btn-secondary {
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-color); color: var(--pico-color); }
}

.btn-danger {
  padding: 0.45rem 1rem;
  background: #ef4444;
  color: #fff;
  border: none;
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { background: #dc2626; }
}
</style>
