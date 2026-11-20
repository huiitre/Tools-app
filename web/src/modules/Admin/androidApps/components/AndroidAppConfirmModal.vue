<script setup lang="ts">
// Confirmation d'une action destructive : le message est fourni par le slot.
defineProps<{
  title: string
  icon: string
  confirmLabel: string
  busyLabel: string
  busy: boolean
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="!busy && emit('cancel')">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title-group">
            <i class="mdi" :class="icon" aria-hidden="true" />
            <span class="modal-title">{{ title }}</span>
          </div>
          <button class="close-btn" :disabled="busy" @click="emit('cancel')">
            <i class="mdi mdi-close" />
          </button>
        </div>

        <div class="modal-sub">
          <slot />
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" :disabled="busy" @click="emit('cancel')">Annuler</button>
          <button class="btn-danger" :disabled="busy" :aria-busy="busy" @click="emit('confirm')">
            {{ busy ? busyLabel : confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
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
  width: 460px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-header { display: flex; align-items: center; justify-content: space-between; }
.modal-title-group { display: flex; align-items: center; gap: 0.5rem; i { color: #ef4444; font-size: 1.2rem; } }
.modal-title { font-size: 1rem; font-weight: 700; }
.modal-sub { font-size: 0.85rem; color: var(--pico-muted-color); :deep(p) { margin: 0 0 0.5rem; } :deep(strong) { color: var(--pico-color); } }
.modal-footer { display: flex; justify-content: flex-end; gap: 0.5rem; }

.close-btn {
  background: none;
  border: none;
  padding: 0.2rem;
  color: var(--pico-muted-color);
  cursor: pointer;
  &:hover:not(:disabled) { color: var(--pico-color); }
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
