<script setup lang="ts">
import { ref } from 'vue'
import type { Workshop } from '@/modules/Dofus/workshop/types/workshop.types'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import toast from '@/services/toast'

const props = defineProps<{ workshop: Workshop }>()
const emit = defineEmits<{ (e: 'close'): void }>()

const store = useWorkshopStore()
const deleting = ref(false)

const confirmDelete = async () => {
  deleting.value = true
  try {
    await store.deleteWorkshop(props.workshop.id)
    emit('close')
  } catch (e: any) {
    toast.error(e instanceof Error ? e.message : 'Erreur lors de la suppression')
  } finally {
    deleting.value = false
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="popup" role="alertdialog" aria-labelledby="delete-workshop-title">
      <div class="icon">
        <i class="mdi mdi-delete-alert-outline" />
      </div>

      <h3 id="delete-workshop-title">Supprimer l'atelier ?</h3>
      <p>
        <strong>{{ workshop.name }}</strong> sera supprimé définitivement, avec ses objets, ses liens et ses
        associations. Cette action est irréversible.
      </p>

      <footer>
        <button type="button" class="btn-ghost" :disabled="deleting" @click="emit('close')">Annuler</button>
        <button type="button" class="btn-danger" :aria-busy="deleting" :disabled="deleting" @click="confirmDelete">
          Supprimer
        </button>
      </footer>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  width: 380px;
  max-width: calc(100vw - 2rem);
  padding: 1.25rem 1.25rem 1rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, .35);
  text-align: center;

  h3 {
    margin: 0 0 0.5rem;
    font-size: 1rem;
    color: var(--pico-contrast);
  }

  p {
    margin: 0;
    font-size: 0.85rem;
    color: var(--pico-muted-color);

    strong {
      color: var(--pico-contrast);
    }
  }
}

.icon {
  display: inline-grid;
  place-items: center;
  width: 2.75rem;
  height: 2.75rem;
  margin-bottom: 0.75rem;
  border-radius: 50%;
  background: color-mix(in srgb, #e53e3e 15%, transparent);
  color: #e53e3e;
  font-size: 1.4rem;
}

footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 1.25rem;
}

.btn-ghost,
.btn-danger {
  width: auto;
  margin: 0;
  padding: 0.4rem 1rem;
  font-size: 0.85rem;
  border-radius: 8px;
}

.btn-ghost {
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  color: inherit;

  &:hover {
    border-color: var(--pico-contrast);
  }
}

.btn-danger {
  background: #e53e3e;
  border: 1px solid #e53e3e;
  color: #fff;

  &:hover {
    background: #c53030;
    border-color: #c53030;
  }
}
</style>
