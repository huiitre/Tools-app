<script setup lang="ts">
import { ref } from 'vue'
import { useClipboard } from '@/composables/useClipboard'
import ItemContextTrigger from '@/modules/Dofus/item/components/ItemContextTrigger.vue'
import PriceAgeColor from '@/modules/Dofus/item/components/PriceAgeColor.vue'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { useWorkshopPriceCalculator } from '@/modules/Dofus/workshop/composables/useWorkshopPriceCalculator'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import { formatNumber, normalizePositiveIntegerInput } from '@/utils/formatNumber'
import { storeToRefs } from 'pinia'
import { useMutationItemPrices } from '@/modules/Dofus/item/fetch/item.fetch'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'
import { getPriceAgeStatus } from '@/modules/Dofus/item/utils/itemPriceSelector'
import { useDofusConfigStore } from '@/modules/Dofus/preferences/preferences.store'

type CraftCard = {
  type: 'main' | 'craft'
  workshopItem: WorkshopItem
  subIngredients?: WorkshopItemIngredient[]
}

const props = defineProps<{
  card: CraftCard
}>()

const store = useWorkshopDetailStore()
const { isOwner, isCondensed } = storeToRefs(store)
const { getUnitPrice } = useWorkshopPriceCalculator()
const { get, set, refreshRecursive } = useItemPrices()
const { copy } = useClipboard()
const { priceDisplayMode } = storeToRefs(useDofusConfigStore())

function getIngredientPriceStatus(ingredient: WorkshopItemIngredient) {
  const prices = get(ingredient.item.id)
  return prices ? getPriceAgeStatus(prices, priceDisplayMode.value) : null
}

const priceUpdateTimeout = ref<ReturnType<typeof setTimeout> | null>(null)

function getIngredientsByParent(
  ingredients: WorkshopItemIngredient[],
  parentId: number | null | undefined
) {
  return ingredients
    .filter(ing => ing.parentIngredientId === parentId)
    .sort((a, b) => {
      if (a.quantityRequired !== b.quantityRequired) {
        return a.quantityRequired - b.quantityRequired
      }
      return a.item.name.localeCompare(b.item.name)
    })
}

function isCraftable(ingredient: WorkshopItemIngredient): boolean {
  return ingredient.item.hasRecipe
}

function isAlreadyCrafted(ingredient: WorkshopItemIngredient): boolean {
  return store.isIngredientCrafted(ingredient.id)
}

async function handleCraftIngredient(ingredient: WorkshopItemIngredient) {
  if (isAlreadyCrafted(ingredient)) return
  
  await store.craftIngredient(props.card.workshopItem.id, ingredient.id)
}

async function handleUpdateIngredientMin(ingredient: WorkshopItemIngredient) {
  if (ingredient.quantityObtained === 0) return

  await store.setIngredientsQuantityObtained([
    { id: ingredient.id, quantity: 0 }
  ])
}

async function handleUpdateIngredientMax(ingredient: WorkshopItemIngredient) {
  const max = ingredient.quantityRequired

  if (ingredient.quantityObtained === max) return

  await store.setIngredientsQuantityObtained([
    { id: ingredient.id, quantity: max }
  ])
}

async function handleUpdateIngredientDelta(
  ingredient: WorkshopItemIngredient,
  delta: number
) {
  const next = ingredient.quantityObtained + delta

  if (next < 0 || next > ingredient.quantityRequired) return
  if (next === ingredient.quantityObtained) return

  await store.setIngredientsQuantityObtained([
    { id: ingredient.id, quantity: next }
  ])
}

async function handleUpdateIngredientInput(
  ingredient: WorkshopItemIngredient,
  event: Event
) {
  const input = event.target as HTMLInputElement
  const value = normalizePositiveIntegerInput(input.value)

  const quantity = Math.min(
    Math.max(0, Number(value)),
    ingredient.quantityRequired
  )

  input.value = String(quantity)

  if (quantity === ingredient.quantityObtained) return

  await store.setIngredientsQuantityObtained([
    { id: ingredient.id, quantity }
  ])
}

function getIngredientPrices(ingredient: WorkshopItemIngredient) {
  const unitPrice = getUnitPrice(ingredient.item.id)
  const totalPrice = unitPrice * ingredient.quantityRequired
  const remaining = Math.max(0, ingredient.quantityRequired - ingredient.quantityObtained)
  const remainingPrice = unitPrice * remaining
  return { unitPrice, totalPrice, remainingPrice }
}

function getUserPrice(itemId: number): number {
  return get(itemId)?.userPrice ?? 0
}

const onPriceInput = (event: Event) => {
  const input = event.target as HTMLInputElement
  const raw = input.value
  const normalized = normalizePositiveIntegerInput(raw)
  const formatted = formatNumber(normalized)
  input.value = formatted
}

async function updateItemPrice(itemId: number, rawValue: string) {
  if (priceUpdateTimeout.value) {
    clearTimeout(priceUpdateTimeout.value)
  }

  priceUpdateTimeout.value = setTimeout(async () => {
    const sanitized = rawValue.replace(/[^0-9]/g, '')
    const newPrice = parseInt(sanitized, 10)

    if (isNaN(newPrice) || newPrice < 0) return

    const oldPrice = getUserPrice(itemId)
    if (newPrice === oldPrice) return

    try {
      await useMutationItemPrices([{ itemId, price: newPrice }])
      set(itemId, newPrice)
      await refreshRecursive([itemId])
    } catch (e) {
      console.error('Erreur update prix:', e)
    }
  }, 500)
}
</script>

<template>
  <section class="item-ingredients">
    <div
      v-for="ingredient in props.card.type === 'main'
        ? getIngredientsByParent(props.card.workshopItem.ingredients, null)
        : props.card.subIngredients"
      :key="ingredient.id"
      :class="['ingredient-wrapper', { complete: ingredient.quantityObtained >= ingredient.quantityRequired }]"
    >
      <div class="ingredient-row">
        <i 
          v-if="isOwner && isCraftable(ingredient)"
          :class="['mdi', 'mdi-hammer-wrench', 'craft-icon', { disabled: isAlreadyCrafted(ingredient) }]"
          @click="handleCraftIngredient(ingredient)"
        />
        
        <ItemContextTrigger :item="ingredient.item">
          <img
            :src="getItemImageByResolution(ingredient.item.images, AssetResolution.X2)?.url"
            :alt="ingredient.item.name"
            class="ing-icon"
          />
        </ItemContextTrigger>

        <div class="ing-info">
          <span class="ing-name copyable" @click="copy(ingredient.item.name)">
            {{ ingredient.item.name }}
          </span>
        </div>

        <div class="ing-controls">
          <button class="ing-btn min" v-if="isOwner" @click="handleUpdateIngredientMin(ingredient)">min</button>
          <button class="ing-btn max" v-if="isOwner" @click="handleUpdateIngredientMax(ingredient)">max</button>
          <button class="ing-btn decrement" v-if="isOwner" @click="handleUpdateIngredientDelta(ingredient, -1)">−</button>
          <input
            type="number"
            :value="ingredient.quantityObtained"
            class="ing-input"
            @input="event => handleUpdateIngredientInput(ingredient, event)"
            :disabled="!isOwner"
          />
          <button class="ing-btn increment" v-if="isOwner" @click="handleUpdateIngredientDelta(ingredient, 1)">+</button>
          <span class="ing-max">/ {{ ingredient.quantityRequired }}</span>
        </div>
      </div>

      <div v-if="!isCondensed" class="price-details">
        <div class="price-left">
          <span>Prix unitaire : <PriceAgeColor :status="getIngredientPriceStatus(ingredient)">{{ getIngredientPrices(ingredient).unitPrice.toLocaleString() }} ₭</PriceAgeColor></span>
          <span>Prix total : {{ getIngredientPrices(ingredient).totalPrice.toLocaleString() }} ₭</span>
          <span>Restant : <span class="colored">{{ getIngredientPrices(ingredient).remainingPrice.toLocaleString() }} ₭</span></span>
        </div>
        <div class="price-right" v-if="isOwner">
          <span class="price-label">Mon prix</span>
          <input
            type="text"
            inputmode="numeric"
            class="ing-input price-input"
            :value="formatNumber(getUserPrice(ingredient.item.id))"
            :disabled="!isOwner"
            @input="(event) => {
              onPriceInput(event)
              const target = event.target as HTMLInputElement
              updateItemPrice(ingredient.item.id, target.value)
            }"
          />
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.item-ingredients {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;

  .ingredient-wrapper {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    padding: 0.25rem;
    border-radius: var(--pico-border-radius);
    transition: background 0.15s;

    &.complete {
      opacity: 0.5;
    }

    &:hover {
      background: var(--pico-card-sectioning-background-color);
    }
  }

  .ingredient-row {
    display: flex;
    grid-template-columns: auto auto 1fr auto;
    gap: 0.4rem;
    align-items: center;
    justify-content: space-between;

    .craft-icon {
      width: 16px;
      font-size: 16px;
      color: var(--pico-primary);
      cursor: pointer;
      transition: color 0.15s;

      &:hover:not(.disabled) {
        color: var(--pico-primary-hover);
      }

      &.disabled {
        color: var(--pico-muted-color);
        opacity: 0.4;
        cursor: not-allowed;
      }
    }

    .ing-icon {
      width: 20px;
      height: 20px;
      object-fit: contain;
      cursor: pointer;
    }

    .ing-info {
      display: flex;
      flex-direction: column;
      gap: 0.1rem;
      min-width: 0;
      flex: 1;
    }

    .ing-name {
      font-size: 0.7rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .ing-controls {
      display: flex;
      align-items: center;
      gap: 0.15rem;

      .ing-btn {
        padding: 0.05rem 0.2rem;
        font-size: 0.6rem;
        line-height: 1.2;
        background: var(--pico-form-element-background-color);
        border: 1px solid var(--pico-form-element-border-color);
        color: var(--pico-form-element-color);
        border-radius: var(--pico-border-radius);
        cursor: pointer;
        transition: all 0.15s;
        margin: 0 !important;
        height: auto;

        &.decrement,
        &.increment {
          font-size: 0.9rem;
        }

        &:hover {
          background: var(--pico-primary-background);
          border-color: var(--pico-primary-border);
          color: var(--pico-primary-inverse);
        }
      }

      .ing-input {
        width: 2.8rem;
        padding: 0.1rem 0.2rem;
        text-align: center;
        font-size: 0.65rem;
        margin: 0 !important;
        height: auto;
      }

      .ing-max {
        font-size: 0.6rem;
        color: var(--pico-muted-color);
        margin: 0 0 0 0.1rem;
      }
    }
  }

  .price-details {
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.5rem;
    font-size: 0.6rem;
    color: var(--pico-muted-color);
    padding-left: 2.4rem;

    .price-left {
      display: flex;
      flex-direction: column;
      gap: 0.1rem;
    }

    .price-right {
      display: flex;
      flex-direction: column;
      gap: 0.1rem;
      align-items: flex-end;
    }

    .price-label {
      font-size: 0.6rem;
      color: var(--pico-muted-color);
    }

    .price-input {
      width: 5rem;
      font-size: 0.6rem;
      padding: 0.1rem 0.2rem;
      margin: 0 !important;
      height: auto;
      text-align: right;
    }
  }

  .colored {
    color: var(--pico-primary);
  }

  input[type="number"]::-webkit-inner-spin-button,
  input[type="number"]::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  input[type="number"] {
    -moz-appearance: textfield;
  }
}
</style>