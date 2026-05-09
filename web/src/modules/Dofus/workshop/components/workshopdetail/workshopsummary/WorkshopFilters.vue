<script setup lang="ts">
import WorkshopItemSearchDropdown from './WorkshopItemSearchDropdown.vue'
import WorkshopLinksButton from './WorkshopLinksButton.vue'
import type { WorkshopSortBy, WorkshopSortOrder } from '../../../store/workshopDetail.store'

interface Props {
  showCompleted: boolean
  condensed: boolean
  displayByZone: boolean
  sortBy: WorkshopSortBy
  sortOrder: WorkshopSortOrder
}

interface Emits {
  (e: 'update:showCompleted', value: boolean): void
  (e: 'update:condensed', value: boolean): void
  (e: 'update:displayByZone', value: boolean): void
  (e: 'update:sortBy', value: WorkshopSortBy): void
  (e: 'update:sortOrder', value: WorkshopSortOrder): void
}

defineProps<Props>()
const emit = defineEmits<Emits>()

const sortOptions: { value: WorkshopSortBy; label: string }[] = [
  { value: 'name', label: 'Nom' },
  { value: 'quantity', label: 'Quantité' },
  { value: 'unitPrice', label: 'Prix Unitaire' },
  { value: 'totalPrice', label: 'Prix Total' },
  { value: 'remainingPrice', label: 'Prix Restant' }
]

function toggleSortOrder(current: WorkshopSortOrder) {
  emit('update:sortOrder', current === 'asc' ? 'desc' : 'asc')
}
</script>

<template>
  <div class="options">
    <div class="checkboxes">
        <label>
            <input 
                type="checkbox" 
                :checked="showCompleted"
                @change="emit('update:showCompleted', ($event.target as HTMLInputElement).checked)"
            >
            Complètes
        </label>
        <label>
            <input 
                type="checkbox" 
                :checked="condensed"
                @change="emit('update:condensed', ($event.target as HTMLInputElement).checked)"
            >
            Condensé
        </label>
        <label>
            <input 
                type="checkbox" 
                :checked="displayByZone"
                @change="emit('update:displayByZone', ($event.target as HTMLInputElement).checked)"
            >
            Zone
        </label>
    </div>

    <div class="sort-controls">
        <select 
            :value="sortBy" 
            @change="emit('update:sortBy', ($event.target as HTMLSelectElement).value as WorkshopSortBy)"
            class="sort-select"
        >
            <option v-for="opt in sortOptions" :key="opt.value" :value="opt.value">
                {{ opt.label }}
            </option>
        </select>
        
        <button
            type="button"
            class="sort-order-btn"
            @click="toggleSortOrder(sortOrder)"
            :title="sortOrder === 'asc' ? 'Croissant' : 'Décroissant'"
        >
            <i class="fa-solid" :class="sortOrder === 'asc' ? 'fa-sort-amount-up' : 'fa-sort-amount-down'"></i>
        </button>

        <WorkshopLinksButton />
    </div>

    <WorkshopItemSearchDropdown />
  </div>
</template>

<style scoped>
.options {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.checkboxes {
    display: flex;
    gap: 0.75rem;
    align-items: center;
}

.options label {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  margin: 0;
  font-size: 0.75rem;
  cursor: pointer;
  white-space: nowrap;
}

.options input[type="checkbox"] {
  margin: 0;
  cursor: pointer;
}

.sort-controls {
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.sort-select {
    margin: 0;
    height: 2rem;
    padding: 0 2rem 0 0.5rem;
    font-size: 0.75rem;
    width: auto;
    min-width: 100px;
}

.sort-order-btn {
    margin: 0;
    height: 2rem;
    width: 2rem;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--pico-form-element-background-color);
    border: 1px solid var(--pico-form-element-border-color);
    color: var(--pico-form-element-color);
    cursor: pointer;
}

.sort-order-btn:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
}
</style>
