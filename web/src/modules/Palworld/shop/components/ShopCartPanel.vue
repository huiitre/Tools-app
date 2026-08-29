<script setup lang="ts">
import { computed, ref } from 'vue'
import { formatNumber } from '@/utils/formatNumber'
import { useShopStore } from '../shop.store'
import { useShopCartStore } from '../shopCart.store'

const shopStore = useShopStore()
const cartStore = useShopCartStore()
const presetName = ref('')

interface EnrichedLine {
  merchantId: string
  itemSlug: string
  quantity: number
  merchantName: string
  itemName: string
  itemIconUrl: string | null
  price: number | null
  currencyItemId: string | null
  currencyName: string | null
  currencyIconUrl: string | null
  available: boolean
}

const enrichedLines = computed<EnrichedLine[]>(() => cartStore.current.map((line) => {
  const merchant = shopStore.merchants.find(m => m.externalId === line.merchantId)
  const offer = merchant?.offers.find(o => o.itemSlug === line.itemSlug)
  return {
    merchantId: line.merchantId,
    itemSlug: line.itemSlug,
    quantity: line.quantity,
    merchantName: merchant?.name ?? merchant?.externalId ?? line.merchantId,
    itemName: offer?.itemName ?? line.itemSlug,
    itemIconUrl: offer?.itemIconUrl ?? null,
    price: offer?.price ?? null,
    currencyItemId: merchant?.currency.itemId ?? null,
    currencyName: merchant?.currency.name ?? null,
    currencyIconUrl: merchant?.currency.iconUrl ?? null,
    available: !!offer,
  }
}))

const totalsByCurrency = computed(() => {
  const totals = new Map<string, { name: string | null, iconUrl: string | null, total: number }>()
  for (const line of enrichedLines.value) {
    if (!line.available || line.price === null || !line.currencyItemId) continue
    const entry = totals.get(line.currencyItemId) ?? { name: line.currencyName, iconUrl: line.currencyIconUrl, total: 0 }
    entry.total += line.price * line.quantity
    totals.set(line.currencyItemId, entry)
  }
  return [...totals.entries()].map(([itemId, v]) => ({ itemId, ...v }))
})

function onSavePreset() {
  if (!presetName.value.trim()) return
  cartStore.savePreset(presetName.value)
  presetName.value = ''
}
</script>

<template>
  <div class="shop-cart">
    <div class="cart-header">
      <h3>Panier</h3>
      <button v-if="cartStore.current.length" type="button" class="clear-btn" @click="cartStore.clearCurrent()">
        <i class="mdi mdi-delete-sweep-outline" /> Vider
      </button>
    </div>

    <ul v-if="enrichedLines.length" class="cart-lines">
      <li v-for="line in enrichedLines" :key="`${line.merchantId}:${line.itemSlug}`" class="cart-line" :class="{ unavailable: !line.available }">
        <img v-if="line.itemIconUrl" :src="line.itemIconUrl" :alt="line.itemName" width="28" height="28" loading="lazy">
        <span v-else class="icon-placeholder"><i class="mdi mdi-help-box-outline" /></span>

        <span class="line-info">
          <span class="line-name">{{ line.itemName }}</span>
          <span class="line-merchant">{{ line.merchantName }}</span>
        </span>

        <input
          type="number"
          min="1"
          :value="line.quantity"
          class="qty-input"
          @input="cartStore.setQuantity(line.merchantId, line.itemSlug, Number(($event.target as HTMLInputElement).value))"
        >

        <span class="line-price">
          <template v-if="line.available">{{ formatNumber(line.price! * line.quantity) }}</template>
          <template v-else>indisponible</template>
        </span>

        <button type="button" class="remove-btn" @click="cartStore.removeLine(line.merchantId, line.itemSlug)">
          <i class="mdi mdi-close" />
        </button>
      </li>
    </ul>
    <p v-else class="empty">Panier vide. Cliquez sur un marchand puis ajoutez des objets.</p>

    <div v-if="totalsByCurrency.length" class="totals">
      <div v-for="t in totalsByCurrency" :key="t.itemId" class="total-row">
        <img v-if="t.iconUrl" :src="t.iconUrl" :alt="t.name ?? t.itemId" width="16" height="16" loading="lazy">
        <strong>{{ formatNumber(t.total) }}</strong> {{ t.name ?? t.itemId }}
      </div>
    </div>

    <div class="presets">
      <h4>Presets</h4>
      <div class="preset-save">
        <input v-model="presetName" type="text" placeholder="Nom du preset..." @keyup.enter="onSavePreset">
        <button type="button" :disabled="!presetName.trim() || !cartStore.current.length" @click="onSavePreset">
          <i class="mdi mdi-content-save-outline" />
        </button>
      </div>
      <ul v-if="cartStore.presetNames.length" class="preset-list">
        <li v-for="name in cartStore.presetNames" :key="name">
          <button type="button" class="preset-load" @click="cartStore.loadPreset(name)">{{ name }}</button>
          <button type="button" class="preset-delete" @click="cartStore.deletePreset(name)"><i class="mdi mdi-delete-outline" /></button>
        </li>
      </ul>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.shop-cart {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.cart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;

  h3 { margin: 0; font-size: 1rem; }
}

.clear-btn {
  margin: 0;
  width: auto;
  padding: 0.3rem 0.6rem;
  font-size: 0.75rem;
  border-radius: var(--pico-border-radius);
  background: transparent;
  border: 1px solid var(--pico-card-border-color);
  color: var(--pico-muted-color);
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
}

.cart-lines {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.cart-line {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.5rem;
  border-radius: 7px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);

  &.unavailable {
    opacity: 0.6;
  }

  img, .icon-placeholder {
    width: 28px;
    height: 28px;
    border-radius: 4px;
    flex-shrink: 0;
    object-fit: cover;
  }

  .icon-placeholder {
    display: grid;
    place-items: center;
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
    color: var(--pico-muted-color);
  }
}

.line-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.line-name {
  font-size: 0.8rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.line-merchant {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.qty-input {
  width: 3.5rem;
  margin: 0;
  height: 1.8rem;
  padding: 0 0.3rem;
  font-size: 0.75rem;
}

.line-price {
  font-size: 0.8rem;
  font-weight: 700;
  min-width: 3.5rem;
  text-align: right;
}

.remove-btn {
  margin: 0;
  width: 1.6rem;
  height: 1.6rem;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  color: var(--pico-muted-color);
  flex-shrink: 0;

  &:hover { color: #e53e3e; }
}

.empty {
  text-align: center;
  color: var(--pico-muted-color);
  padding: 1.5rem 0;
  font-size: 0.85rem;
}

.totals {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.6rem;
  border-radius: 8px;
  background: color-mix(in srgb, var(--pico-primary) 8%, transparent);
}

.total-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;

  img { border-radius: 3px; }
  strong { color: var(--pico-primary); }
}

.presets {
  border-top: 1px solid var(--pico-card-border-color);
  padding-top: 0.75rem;

  h4 { margin: 0 0 0.5rem; font-size: 0.85rem; color: var(--pico-muted-color); }
}

.preset-save {
  display: flex;
  gap: 0.4rem;

  input {
    margin: 0;
    height: 2rem;
    font-size: 0.8rem;
  }

  button {
    margin: 0;
    width: 2rem;
    height: 2rem;
    padding: 0;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

.preset-list {
  list-style: none;
  margin: 0.5rem 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;

  li {
    display: flex;
    align-items: center;
    gap: 0.3rem;
  }
}

.preset-load {
  flex: 1;
  margin: 0;
  text-align: left;
  padding: 0.35rem 0.6rem;
  font-size: 0.78rem;
  border-radius: 6px;
  background: transparent;
  border: 1px solid var(--pico-card-border-color);
  color: inherit;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

.preset-delete {
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

  &:hover { color: #e53e3e; }
}
</style>
