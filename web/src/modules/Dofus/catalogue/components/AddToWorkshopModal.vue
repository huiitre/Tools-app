<script setup lang="ts">
import { ref, computed } from 'vue'
import { useWorkshopStore } from '@/modules/Dofus/workshop/store/workshop.store'
import { useAddItemsToWorkshop } from '@/modules/Dofus/workshop/fetch/workshopItem.fetch'
import toast from '@/services/toast'

const props = defineProps<{
  itemIds: number[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const workshopStore = useWorkshopStore()
const selectedWorkshopIds = ref<Set<number>>(new Set())

const workshops = computed(() => workshopStore.workshops)
const itemCount = computed(() => props.itemIds.length)

function toggleWorkshop(id: number) {
  if (selectedWorkshopIds.value.has(id)) {
    selectedWorkshopIds.value.delete(id)
  } else {
    selectedWorkshopIds.value.add(id)
  }
}

async function addToWorkshops() {
  if (selectedWorkshopIds.value.size === 0) {
    toast.warning('Sélectionnez au moins un atelier')
    return
  }

  const results = {
    success: 0,
    errors: [] as string[]
  }

  for (const workshopId of selectedWorkshopIds.value) {
    try {
      await useAddItemsToWorkshop(workshopId, props.itemIds)
      results.success++
    } catch (e: any) {
      const workshopName = workshops.value.find(w => w.id === workshopId)?.name || `#${workshopId}`
      const errorMsg = e?.response?.data?.message || e?.message || 'Erreur inconnue'
      results.errors.push(`${workshopName}: ${errorMsg}`)
    }
  }

  if (results.success > 0) {
    const itemText = itemCount.value > 1 ? `${itemCount.value} objets ajoutés` : 'Objet ajouté'
    toast.success(`${itemText} à ${results.success} atelier(s)`)
  }

  if (results.errors.length > 0) {
    results.errors.forEach(err => toast.error(err))
  }

  emit('close')
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <h3 class="modal-title">
        Ajouter {{ itemCount }} objet{{ itemCount > 1 ? 's' : '' }} à un atelier
      </h3>

      <div class="modal-content">
        <div
          v-for="workshop in workshops"
          :key="workshop.id"
          class="workshop-item"
          @click="toggleWorkshop(workshop.id)"
        >
          <input
            type="checkbox"
            :checked="selectedWorkshopIds.has(workshop.id)"
            @click.stop="toggleWorkshop(workshop.id)"
          />
          <span class="workshop-name">{{ workshop.name }}</span>
        </div>

        <div v-if="workshops.length === 0" class="empty">
          Aucun atelier disponible
        </div>
      </div>

      <div class="modal-actions">
        <button @click="emit('close')" class="secondary">
          Annuler
        </button>
        <button @click="addToWorkshops">
          Ajouter
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  width: 400px;
  max-width: 90vw;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  font-size: 0.85rem;
}

.modal-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.modal-content {
  padding: 0.5rem;
  overflow-y: auto;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.workshop-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  cursor: pointer;
  border-radius: var(--pico-border-radius);
  transition: background 0.15s;

  &:hover {
    background: var(--pico-card-sectioning-background-color);
  }

  input[type="checkbox"] {
    margin: 0;
    cursor: pointer;
  }

  .workshop-name {
    flex: 1;
  }
}

.empty {
  padding: 1rem;
  text-align: center;
  color: var(--pico-muted-color);
}

.modal-actions {
  display: flex;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-top: 1px solid var(--pico-muted-border-color);

  button {
    flex: 1;
    margin: 0;
    font-size: 0.85rem;
  }
}
</style>