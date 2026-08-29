<script setup lang="ts">
import { computed, ref } from 'vue'
import { normalizeSearchText } from '@/utils/searchNormalize'
import { useShopStore } from '../shop.store'
import type { Merchant } from '../types/shop.types'

const props = defineProps<{
  merchants: Merchant[]
  selectedId: string | null
}>()

const emit = defineEmits<{ select: [id: string] }>()

const shopStore = useShopStore()
const searchQuery = ref('')

const normalizedQuery = computed(() => normalizeSearchText(searchQuery.value.trim()))

const sortedMerchants = computed(() => {
  const favorites: Merchant[] = []
  const rest: Merchant[] = []
  for (const merchant of props.merchants) {
    (shopStore.isFavorite(merchant.externalId) ? favorites : rest).push(merchant)
  }
  return [...favorites, ...rest]
})

function merchantLabel(merchant: Merchant): string {
  if (merchant.name) return merchant.name
  const suffix = merchant.externalId.match(/(\d+)$/)?.[1]
  return suffix ? `Marchand ambulant #${suffix}` : 'Marchand ambulant'
}

function matchesSearch(merchant: Merchant): boolean {
  if (!normalizedQuery.value) return true
  return merchant.offers.some(offer => normalizeSearchText(offer.itemName).includes(normalizedQuery.value))
}

const matchCount = computed(() => props.merchants.filter(matchesSearch).length)
</script>

<template>
  <div class="merchant-panel">
    <input
      v-model="searchQuery"
      type="search"
      placeholder="Rechercher un objet..."
      class="search-input"
    >
    <span v-if="normalizedQuery" class="match-count">{{ matchCount }} / {{ merchants.length }} marchands</span>

    <ul class="merchant-list">
      <li v-for="merchant in sortedMerchants" :key="merchant.externalId">
        <div
          class="merchant-item"
          :class="{ active: merchant.externalId === selectedId, dimmed: !matchesSearch(merchant) }"
        >
          <button
            type="button"
            class="favorite-btn"
            :class="{ active: shopStore.isFavorite(merchant.externalId) }"
            :title="shopStore.isFavorite(merchant.externalId) ? 'Retirer des favoris' : 'Ajouter aux favoris'"
            @click.stop="shopStore.toggleFavorite(merchant.externalId)"
          >
            <i :class="shopStore.isFavorite(merchant.externalId) ? 'mdi mdi-star' : 'mdi mdi-star-outline'" />
          </button>

          <button type="button" class="merchant-select" @click="emit('select', merchant.externalId)">
            <img v-if="merchant.portraitUrl" :src="merchant.portraitUrl" :alt="merchantLabel(merchant)" width="40" height="40" loading="lazy">
            <span v-else class="portrait-placeholder"><i class="mdi mdi-account" /></span>

            <span class="merchant-name" :class="{ generic: !merchant.name }">{{ merchantLabel(merchant) }}</span>
          </button>

          <span class="merchant-currency">
            <img v-if="merchant.currency.iconUrl" :src="merchant.currency.iconUrl" :alt="merchant.currency.name ?? merchant.currency.itemId" width="10" height="10" loading="lazy">
            <i v-else class="mdi mdi-currency-usd" />
            {{ merchant.currency.name ?? merchant.currency.itemId }}
          </span>
        </div>
      </li>
    </ul>
  </div>
</template>

<style lang="scss" scoped>
.merchant-panel {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  height: 100%;
  min-height: 0;
}

.search-input {
  margin: 0;
  flex-shrink: 0;
  height: 2rem;
  font-size: 0.78rem;
}

.match-count {
  flex-shrink: 0;
  margin-top: -0.4rem;
  font-size: 0.68rem;
  color: var(--pico-muted-color);
}

.merchant-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

.merchant-item {
  position: relative;
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.4rem 1rem 0.3rem;
  border-radius: 8px;
  border: 1px solid transparent;
  transition: background 0.15s ease, border-color 0.15s ease, opacity 0.15s ease, filter 0.15s ease;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
  }

  &.active {
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
  }

  &.dimmed {
    opacity: 0.35;
    filter: grayscale(0.8);
  }
}

.favorite-btn {
  margin: 0;
  width: 1.8rem;
  height: 1.8rem;
  padding: 0;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover {
    color: #f5b301;
  }

  &.active {
    color: #f5b301;
  }
}

.merchant-select {
  flex: 1;
  min-width: 0;
  margin: 0;
  padding: 0.2rem 0.2rem;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  background: transparent;
  border: none;
  color: inherit;
  cursor: pointer;
  text-align: left;

  img, .portrait-placeholder {
    width: 40px;
    height: 40px;
    border-radius: 6px;
    flex-shrink: 0;
    object-fit: cover;
  }

  .portrait-placeholder {
    display: grid;
    place-items: center;
    background: var(--pico-card-background-color);
    color: var(--pico-muted-color);
    font-size: 1.1rem;
  }
}

.merchant-name {
  flex: 1;
  min-width: 0;
  font-size: 0.85rem;
  line-height: 1.25;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;

  &.generic {
    color: var(--pico-muted-color);
    font-style: italic;
  }
}

.merchant-currency {
  position: absolute;
  bottom: 0.15rem;
  right: 0.5rem;
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  font-size: 0.6rem;
  line-height: 1;
  color: var(--pico-muted-color);
  pointer-events: none;

  img { border-radius: 2px; }
}
</style>
