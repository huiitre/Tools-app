<script setup lang="ts">
import { ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import { useShopStore } from '../shop.store'
import ItemPurchaseModal from './ItemPurchaseModal.vue'
import type { Merchant, MerchantOffer } from '../types/shop.types'

const props = defineProps<{
  merchant: Merchant
}>()

const emit = defineEmits<{ addToCart: [itemSlug: string, quantity: number] }>()

const shopStore = useShopStore()
const selectedOffer = ref<MerchantOffer | null>(null)

function betterElsewhere(itemSlug: string, price: number) {
  const elsewhere = shopStore.merchantsSellingItem(itemSlug).filter(e => e.merchant.externalId !== props.merchant.externalId)
  if (!elsewhere.length) return null
  const cheapest = elsewhere[0]
  return cheapest.offer.price < price ? cheapest : null
}

function confirmPurchase(quantity: number) {
  if (!selectedOffer.value) return
  emit('addToCart', selectedOffer.value.itemSlug, quantity)
  selectedOffer.value = null
}
</script>

<template>
  <div class="merchant-offers">
    <div class="offers-header">
      <span v-if="merchant.currency.iconUrl" class="currency">
        <img :src="merchant.currency.iconUrl" :alt="merchant.currency.name ?? merchant.currency.itemId" width="18" height="18" loading="lazy">
        {{ merchant.currency.name }}
      </span>
      <span v-else class="currency">
        <i class="mdi mdi-currency-usd" /> {{ merchant.currency.name }}
      </span>
      <span v-if="merchant.restockMinute" class="restock">
        <i class="mdi mdi-clock-outline" /> Réappro toutes les {{ merchant.restockMinute }} min
      </span>
    </div>

    <div class="offer-grid">
      <button
        v-for="offer in merchant.offers"
        :key="offer.itemId"
        type="button"
        class="offer-card"
        @click="selectedOffer = offer"
      >
        <span v-if="offer.productType === 'ONLY_PURCHASE_ONE'" class="badge-unique" title="Achetable une seule fois">unique</span>
        <span v-if="betterElsewhere(offer.itemSlug, offer.price)" class="cheaper-hint" :title="`Moins cher chez ${betterElsewhere(offer.itemSlug, offer.price)!.merchant.name ?? 'un autre marchand'} (${formatNumber(betterElsewhere(offer.itemSlug, offer.price)!.offer.price)})`">
          <i class="mdi mdi-arrow-down-bold-circle-outline" />
        </span>

        <img v-if="offer.itemIconUrl" :src="offer.itemIconUrl" :alt="offer.itemName" width="64" height="64" loading="lazy">
        <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>

        <span class="offer-name">{{ offer.itemName }}</span>
        <span class="offer-price">{{ formatNumber(offer.price) }}</span>
      </button>
    </div>

    <p v-if="!merchant.offers.length" class="empty">Ce marchand ne vend rien pour le moment.</p>

    <ItemPurchaseModal
      v-if="selectedOffer"
      :offer="selectedOffer"
      :currency="merchant.currency"
      @close="selectedOffer = null"
      @confirm="confirmPurchase"
    />
  </div>
</template>

<style lang="scss" scoped>
.merchant-offers {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.offers-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-size: 0.8rem;
  color: var(--pico-muted-color);

  .currency {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    font-weight: 600;
    color: var(--pico-color);

    img { border-radius: 3px; }
  }

  .restock {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
  }
}

.offer-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 1px;
  background: var(--pico-card-border-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;
  overflow: hidden;
}

.offer-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3rem;
  padding: 0.6rem 0.4rem;
  background: var(--pico-card-background-color);
  border: none;
  cursor: pointer;
  transition: background 0.15s ease;

  &:hover {
    background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
  }

  img, .icon-placeholder {
    width: 48px;
    height: 48px;
    border-radius: 5px;
    object-fit: cover;
  }

  .icon-placeholder {
    display: grid;
    place-items: center;
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
    color: var(--pico-muted-color);
    font-size: 1.2rem;
  }
}

.offer-name {
  font-size: 0.78rem;
  text-align: center;
  line-height: 1.25;
  color: var(--pico-color);
  min-height: 2rem;
}

.offer-price {
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--pico-primary);
}

.badge-unique {
  position: absolute;
  top: 0.4rem;
  left: 0.4rem;
  font-size: 0.6rem;
  text-transform: uppercase;
  font-weight: 700;
  padding: 0.1rem 0.35rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
}

.cheaper-hint {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
  color: #2ecc71;
  display: flex;
  align-items: center;
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 2rem 0;
}
</style>
