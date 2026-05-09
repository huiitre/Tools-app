<script setup lang="ts">
import { ref } from 'vue'
import { useAppUpdate } from '@/composables/useAppUpdate'
import { useEnv } from '@/composables/useEnv'

const { updateAvailable, applyUpdate } = useAppUpdate()
const { isElectron } = useEnv()

const showModal = ref(false)
</script>

<template>
  <button
    v-if="updateAvailable"
    class="update-button"
    title="Mise à jour disponible"
    aria-label="Mise à jour disponible"
    @click="showModal = true"
  >
    <i class="mdi mdi-update" aria-hidden="true"></i>
    <span class="update-label">Mise à jour</span>
  </button>

  <Teleport to="body">
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal" role="dialog" aria-modal="true">
        <div class="modal-header">
          <i class="mdi mdi-update"></i>
          <h2>Nouvelle version disponible</h2>
        </div>

        <div class="modal-body">
          <p>Une nouvelle version de l'application est disponible.</p>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="showModal = false">Plus tard</button>
          <button class="btn-primary" @click="applyUpdate">
            {{ isElectron ? 'Installer' : 'Recharger la page' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.update-button {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;

  height: 2.25rem;
  padding: 0 0.5rem;

  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  background: transparent;
  cursor: pointer;
  color: inherit;
}

.update-button i {
  font-size: 1.25rem;
  color: #0ce114;
}

.update-button:hover {
  background: var(--pico-muted-background-color);
}

.update-label {
  display: none;
  font-size: 0.85rem;
}

@media (min-width: 768px) {
  .update-label {
    display: inline;
  }
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.modal {
  background: var(--pico-card-background-color);
  border-radius: var(--pico-border-radius);
  border: 1px solid var(--pico-muted-border-color);
  width: 100%;
  max-width: 420px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.modal-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.modal-header i {
  font-size: 1.75rem;
  color: #0ce114;
}

.modal-header h2 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 600;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

.btn-primary {
  padding: 0.5rem 1rem;
  border-radius: var(--pico-border-radius);
  border: none;
  background: var(--pico-primary);
  cursor: pointer;
  font-size: 0.9rem;
}

.btn-primary:hover {
  background: var(--pico-primary-hover);
}

.btn-secondary {
  padding: 0.5rem 1rem;
  border-radius: var(--pico-border-radius);
  border: 1px solid var(--pico-muted-border-color);
  background: transparent;
  cursor: pointer;
  font-size: 0.9rem;
  color: inherit;
}

.btn-secondary:hover {
  background: var(--pico-muted-background-color);
}
</style>