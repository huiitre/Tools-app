<script setup lang="ts">
import { computed, ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import type { ItemCatalogEntry } from '../types/items.types'

const SLIDER_MAX = 9999

const props = defineProps<{
  item: ItemCatalogEntry
}>()

const emit = defineEmits<{
  close: []
}>()

const quantity = ref(1)

// Règle donnée par l'utilisateur : 10% du prix d'achat, pour l'instant (pas de donnée réelle de vente
// dans les assets extraits).
const unitSellPrice = computed(() => Math.floor((props.item.price ?? 0) * 0.1))
const totalSellPrice = computed(() => unitSellPrice.value * quantity.value)

function setQuantity(value: number) {
  quantity.value = Math.max(0, Math.round(value))
}

function onQuantityInput(event: Event) {
  setQuantity(Number((event.target as HTMLInputElement).value))
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('close')">
      <div class="modal">
        <button type="button" class="close-btn" @click="emit('close')"><i class="mdi mdi-close" /></button>

        <div class="item-preview">
          <img v-if="item.iconUrl" :src="item.iconUrl" :alt="item.name" width="96" height="96">
          <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>
          <h3 class="item-name">{{ item.name }}</h3>
          <span class="badge-simulation">Simulation de vente</span>
        </div>

        <div class="quantity-control">
          <button type="button" @click="setQuantity(0)"><i class="mdi mdi-chevron-double-left" /></button>
          <button type="button" @click="setQuantity(quantity - 1)"><i class="mdi mdi-chevron-left" /></button>
          <input
            type="number"
            min="0"
            class="quantity-input"
            :value="quantity"
            @input="onQuantityInput"
          >
          <button type="button" @click="setQuantity(quantity + 1)"><i class="mdi mdi-chevron-right" /></button>
          <button type="button" @click="setQuantity(SLIDER_MAX)"><i class="mdi mdi-chevron-double-right" /></button>
        </div>

        <div class="quantity-slider">
          <span>0</span>
          <input
            type="range"
            min="0"
            :max="SLIDER_MAX"
            :value="Math.min(quantity, SLIDER_MAX)"
            @input="onQuantityInput"
          >
          <span>{{ formatNumber(SLIDER_MAX) }}</span>
        </div>

        <div class="total-row">
          <span>Vente totale (10% du prix d'achat)</span>
          <span class="total-price">{{ formatNumber(totalSellPrice) }}</span>
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
  position: relative;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.65rem;
  padding: 1.5rem;
  width: 360px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
  box-shadow: var(--pico-card-box-shadow);
}

.close-btn {
  position: absolute;
  top: 0.6rem;
  right: 0.6rem;
  margin: 0;
  width: 1.8rem;
  height: 1.8rem;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover { color: var(--pico-color); }
}

.item-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;

  img, .icon-placeholder {
    width: 96px;
    height: 96px;
    border-radius: 10px;
    object-fit: cover;
  }

  .icon-placeholder {
    display: grid;
    place-items: center;
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
    color: var(--pico-muted-color);
    font-size: 2rem;
  }
}

.item-name {
  margin: 0;
  font-size: 1rem;
  text-align: center;
}

.badge-simulation {
  font-size: 0.7rem;
  text-transform: uppercase;
  font-weight: 700;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in srgb, #d4af37 20%, transparent);
  color: #d4af37;
}

.quantity-control {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;

  button {
    margin: 0;
    width: 2rem;
    height: 2rem;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: var(--pico-border-radius);
    background: var(--pico-form-element-background-color);
    border: 1px solid var(--pico-form-element-border-color);
    color: var(--pico-color);
    cursor: pointer;
    flex-shrink: 0;

    &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
  }
}

.quantity-input {
  min-width: 0;
  width: 6rem;
  margin: 0;
  text-align: center;
  font-family: monospace;
  font-size: 1.1rem;
  font-weight: 700;
  height: 2rem;
  padding: 0 0.3rem;
}

.quantity-slider {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);

  input[type="range"] { margin: 0; flex: 1; }
}

.total-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.6rem 0.8rem;
  border-radius: 8px;
  background: color-mix(in srgb, #d4af37 10%, transparent);
  font-size: 0.85rem;
}

.total-price {
  font-weight: 700;
  font-size: 1rem;
  color: #d4af37;
}
</style>
