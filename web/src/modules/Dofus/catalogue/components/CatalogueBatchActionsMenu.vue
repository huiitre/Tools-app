<script setup lang="ts">
import { computed } from 'vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'

const emit = defineEmits<{
  (e: 'addToWorkshop'): void
}>()

const catalogueStore = useCatalogueStore()
const selectedCount = computed(() => catalogueStore.selectedItemsWithRecipe.length)
</script>

<template>
  <div class="batch-actions-panel">
    <h3 class="panel-title">
      {{ selectedCount }} objet{{ selectedCount > 1 ? 's' : '' }} sélectionné{{ selectedCount > 1 ? 's' : '' }}
    </h3>

    <div class="action-block">
      <button
        class="action-button"
        @click="emit('addToWorkshop')"
      >
        <i class="mdi mdi-plus-circle"></i>
        <span>Ajouter à l'atelier</span>
      </button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.batch-actions-panel {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-width: 220px;
  font-size: 0.85rem;
}

.panel-title {
  font-size: 0.9rem;
  font-weight: 600;
  margin: 0;
  color: var(--pico-primary);
}

.action-block {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.action-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.65rem;
  background: var(--pico-card-sectioning-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  cursor: pointer;
  transition: all 0.15s;
  color: var(--pico-color);
  font-size: 0.85rem;
  text-align: left;
  width: 100%;

  &:hover {
    background: var(--pico-primary-background);
    border-color: var(--pico-primary-border);
    color: var(--pico-primary-inverse);
  }

  i {
    font-size: 1rem;
    opacity: 0.8;
  }
}
</style>