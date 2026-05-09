<script setup lang="ts">
import { ref } from 'vue'
import type { AdminRole, AdminUser } from '../../users/types/adminUsers.types'

const props = defineProps<{ user: AdminUser; roles: AdminRole[] }>()
const emit = defineEmits<{
  confirm: [role: AdminRole]
  cancel: []
}>()

const picked = ref<AdminRole | null>(null)
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('cancel')">
      <div class="modal">
        <h3 class="modal-title">Ajouter {{ props.user.name }}</h3>
        <p class="modal-sub">Choisissez le rôle de cet utilisateur dans ce module.</p>

        <div class="role-grid">
          <button
            v-for="r in props.roles"
            :key="r.id"
            class="role-btn"
            :class="{ selected: picked?.id === r.id }"
            @click="picked = r"
          >
            {{ r.name }}
          </button>
        </div>

        <div class="modal-actions">
          <button class="btn-secondary" @click="emit('cancel')">Annuler</button>
          <button class="btn-primary" :disabled="!picked" @click="picked && emit('confirm', picked)">
            Confirmer
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
  width: 420px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-title { font-size: 1rem; font-weight: 700; margin: 0; }
.modal-sub { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

.role-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.4rem;
}

.role-btn {
  padding: 0.45rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  background: none;
  font-size: 0.8rem;
  cursor: pointer;
  transition: all 0.12s;
  &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
  &.selected { background: var(--pico-primary); color: var(--pico-primary-inverse); border-color: var(--pico-primary); }
}

.btn-primary {
  padding: 0.45rem 1rem;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { opacity: 0.85; }
}

.btn-secondary {
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:hover { border-color: var(--pico-color); color: var(--pico-color); }
}
</style>
