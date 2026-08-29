<script setup lang="ts">
import { computed, ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import type { MerchantOffer, ShopCurrency } from '../types/shop.types'

const props = defineProps<{
  offer: MerchantOffer
  currency: ShopCurrency
}>()

const emit = defineEmits<{
  close: []
  confirm: [quantity: number]
}>()

const maxQuantity = props.offer.productType === 'ONLY_PURCHASE_ONE' ? 1 : (props.offer.itemMaxStackCount ?? 9999)
const quantity = ref(Math.min(1, maxQuantity))

const paddedQuantity = computed(() => String(quantity.value).padStart(4, '0'))
const totalPrice = computed(() => props.offer.price * quantity.value)

function setQuantity(value: number) {
  quantity.value = Math.max(0, Math.min(maxQuantity, Math.round(value)))
}

function confirm() {
  if (quantity.value <= 0) return
  emit('confirm', quantity.value)
}
</script>

<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="emit('close')">
      <div class="modal">
        <button type="button" class="close-btn" @click="emit('close')"><i class="mdi mdi-close" /></button>

        <div class="item-preview">
          <img v-if="offer.itemIconUrl" :src="offer.itemIconUrl" :alt="offer.itemName" width="96" height="96">
          <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>
          <h3 class="item-name">{{ offer.itemName }}</h3>
          <span v-if="offer.productType === 'ONLY_PURCHASE_ONE'" class="badge-unique">Achat unique</span>
        </div>

        <div class="quantity-control">
          <button type="button" @click="setQuantity(0)"><i class="mdi mdi-chevron-double-left" /></button>
          <button type="button" @click="setQuantity(quantity - 1)"><i class="mdi mdi-chevron-left" /></button>
          <span class="quantity-value">{{ paddedQuantity }}</span>
          <button type="button" @click="setQuantity(quantity + 1)"><i class="mdi mdi-chevron-right" /></button>
          <button type="button" @click="setQuantity(maxQuantity)"><i class="mdi mdi-chevron-double-right" /></button>
        </div>

        <div class="quantity-slider">
          <span>0</span>
          <input
            type="range"
            min="0"
            :max="maxQuantity"
            :value="quantity"
            @input="setQuantity(Number(($event.target as HTMLInputElement).value))"
          >
          <span>{{ formatNumber(maxQuantity) }}</span>
        </div>

        <div class="total-row">
          <span>Prix total</span>
          <span class="total-price">
            <img v-if="currency.iconUrl" :src="currency.iconUrl" :alt="currency.name ?? currency.itemId" width="18" height="18">
            {{ formatNumber(totalPrice) }} {{ currency.name }}
          </span>
        </div>

        <button type="button" class="buy-btn" :disabled="quantity <= 0" @click="confirm">
          Ajouter au panier
        </button>
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

  &:hover { color: inherit; }
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

.badge-unique {
  font-size: 0.7rem;
  text-transform: uppercase;
  font-weight: 700;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
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
    color: var(--app-text-color);
    cursor: pointer;

    &:hover { border-color: var(--pico-primary); color: var(--pico-primary); }
  }
}

.quantity-value {
  min-width: 4.5rem;
  text-align: center;
  font-family: monospace;
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: 0.05em;
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
  padding: 0.6rem 0.8rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
  font-size: 0.9rem;
}

.total-price {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-weight: 700;
  color: var(--pico-primary);

  img { border-radius: 3px; }
}

.buy-btn {
  margin: 0;
  padding: 0.6rem;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: 0.35rem;
  font-size: 0.9rem;
  font-weight: 700;
  cursor: pointer;

  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { opacity: 0.85; }
}
</style>
