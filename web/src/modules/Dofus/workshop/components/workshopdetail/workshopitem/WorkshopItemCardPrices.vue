<script setup lang="ts">
import { computed, ref } from 'vue'
import { useWorkshopPriceCalculator } from '@/modules/Dofus/workshop/composables/useWorkshopPriceCalculator'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import { formatNumber, normalizePositiveIntegerInput } from '@/utils/formatNumber'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { storeToRefs } from 'pinia'
import { useMutationItemPrices } from '@/modules/Dofus/item/fetch/item.fetch'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'

type CraftCard = {
  type: 'main' | 'craft'
  workshopItem: WorkshopItem
  craftedIngredient?: WorkshopItemIngredient
}

const props = defineProps<{
  card: CraftCard
}>()

const { getUnitPrice } = useWorkshopPriceCalculator()

const workshopDetailStore = useWorkshopDetailStore()
const { isOwner } = storeToRefs(workshopDetailStore)

const { refreshRecursive } = useItemPrices()

const priceUpdateTimeout = ref<ReturnType<typeof setTimeout> | null>(null)

function getFormattedUnitPrice(itemId: number): string {
  return formatNumber(getUnitPrice(itemId))
}

async function updateItemPrice(itemId: number, rawValue: string) {
  // Clear timeout précédent
  if (priceUpdateTimeout.value) {
    clearTimeout(priceUpdateTimeout.value)
  }

  // Debounce de 800ms
  priceUpdateTimeout.value = setTimeout(async () => {
    const sanitized = rawValue.replace(/[^0-9]/g, '')
    const newPrice = parseInt(sanitized, 10)

    if (isNaN(newPrice) || newPrice < 0) {
      return
    }

    const oldPrice = getUnitPrice(itemId)
    if (newPrice === oldPrice) {
      return
    }

    try {
      await useMutationItemPrices([{ itemId, price: newPrice }])
      await refreshRecursive([itemId])
    } catch (e) {
      console.error('Erreur update prix:', e)
    }
  }, 500)
}

const unitPriceMultiplied = computed(() => {
  if (props.card.type === 'main') {
    return getUnitPrice(props.card.workshopItem.item.id) * props.card.workshopItem.quantity
  } else {
    return getUnitPrice(props.card.craftedIngredient!.item.id) * props.card.craftedIngredient!.quantityRequired
  }
})

// Prix craft UNITAIRE = somme PU TOUS les ingrédients directs (craftés ou non)
const craftPrice = computed(() => {
  if (props.card.type === 'main') {
    const directIngredients = props.card.workshopItem.ingredients.filter(ing => !ing.parentIngredientId)
    
    return directIngredients.reduce((sum, ing) => {
      const baseQty = ing.quantityRequired / props.card.workshopItem.quantity
      return sum + (getUnitPrice(ing.item.id) * baseQty)
    }, 0)
  } else {
    const subIngredients = props.card.workshopItem.ingredients.filter(
      sub => sub.parentIngredientId === props.card.craftedIngredient!.id
    )
    
    const parentQty = props.card.craftedIngredient!.quantityRequired
    
    return subIngredients.reduce((sum, sub) => {
      const baseQty = sub.quantityRequired / parentQty
      return sum + (getUnitPrice(sub.item.id) * baseQty)
    }, 0)
  }
})

// Prix craft multiplié
const craftPriceMultiplied = computed(() => {
  if (props.card.type === 'main') {
    return craftPrice.value * props.card.workshopItem.quantity
  } else {
    return craftPrice.value * props.card.craftedIngredient!.quantityRequired
  }
})

// Comparaison pour indicateur visuel
const comparison = computed(() => {
  if (unitPriceMultiplied.value < craftPriceMultiplied.value) {
    return 'buy' // Acheter est moins cher
  } else if (craftPriceMultiplied.value < unitPriceMultiplied.value) {
    return 'craft' // Crafter est moins cher
  } else {
    return 'equal' // Égaux
  }
})

// Prix total craft = prix craft multiplié - PU des validés
const totalCraftPrice = computed(() => {
  if (props.card.type === 'main') {
    const directIngredients = props.card.workshopItem.ingredients.filter(ing => !ing.parentIngredientId)
    
    const totalObtained = directIngredients.reduce((sum, ing) => {
      return sum + (getUnitPrice(ing.item.id) * ing.quantityObtained)
    }, 0)
    
    return craftPriceMultiplied.value - totalObtained
  } else {
    const subIngredients = props.card.workshopItem.ingredients.filter(
      sub => sub.parentIngredientId === props.card.craftedIngredient!.id
    )
    
    const totalObtained = subIngredients.reduce((sum, sub) => {
      return sum + (getUnitPrice(sub.item.id) * sub.quantityObtained)
    }, 0)
    
    return craftPriceMultiplied.value - totalObtained
  }
})

const onInput = (event: Event) => {
  const input = event.target as HTMLInputElement
  const raw = input.value
  const normalized = normalizePositiveIntegerInput(raw)
  const formatted = formatNumber(normalized)
  input.value = formatted
}

</script>

<template>
  <section class="item-prices">

    <!-- LIGNE 1 -->
    <div class="row row-unit">
      <div class="cell">
        <span class="price-label">Prix unitaire</span>
        <input
          type="text"
          inputmode="numeric"
          :value="getFormattedUnitPrice(
            props.card.type === 'main'
              ? props.card.workshopItem.item.id
              : props.card.craftedIngredient!.item.id
          )"
          class="price-input"
          @input="(event) => {
            onInput(event)
            const target = event.target as HTMLInputElement
            const itemId = props.card.type === 'main'
              ? props.card.workshopItem.item.id
              : props.card.craftedIngredient!.item.id
            updateItemPrice(itemId, target.value)
          }"
          :disabled="!isOwner"
        />
      </div>

      <div class="cell value" :class="{ cheaper: comparison === 'buy', expensive: comparison === 'craft' }">
        <span class="price-label">Prix unitaire multiplié</span>
        <strong class="price-value">
          <i v-if="comparison === 'buy'" class="mdi mdi-arrow-down indicator" />
          <i v-else-if="comparison === 'craft'" class="mdi mdi-arrow-up indicator" />
          {{ unitPriceMultiplied.toLocaleString() }} ₭
        </strong>
      </div>
    </div>

    <!-- LIGNE 2 -->
    <div class="row row-craft">
      <div class="cell value">
        <span class="price-label">Prix craft</span>
        <strong class="price-value">
          {{ craftPrice.toLocaleString() }} ₭
        </strong>
      </div>

      <div class="cell value" :class="{ cheaper: comparison === 'craft', expensive: comparison === 'buy' }">
        <span class="price-label">Prix craft multiplié</span>
        <strong class="price-value">
          <i v-if="comparison === 'craft'" class="mdi mdi-arrow-down indicator" />
          <i v-else-if="comparison === 'buy'" class="mdi mdi-arrow-up indicator" />
          {{ craftPriceMultiplied.toLocaleString() }} ₭
        </strong>
      </div>

      <div class="cell value highlight">
        <span class="price-label">Prix total craft</span>
        <strong class="price-value">
          {{ totalCraftPrice.toLocaleString() }} ₭
        </strong>
      </div>
    </div>

  </section>
</template>

<style scoped>
.item-prices {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  padding: 0.5rem;
  background: var(--pico-card-sectioning-background-color);
  border-radius: var(--pico-border-radius);
  font-size: 0.75rem;

  .row {
    display: grid;
    gap: 0.5rem;

    &.row-unit {
      grid-template-columns: 1fr 1fr;
    }

    &.row-craft {
      grid-template-columns: repeat(3, 1fr);
    }
  }

  .cell {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    align-items: center;

    &.cheaper {
      .price-value {
        color: var(--pico-ins-color);
      }
    }

    &.expensive {
      .price-value {
        color: var(--pico-del-color);
      }
    }

    &.highlight {
      .price-value {
        color: var(--pico-primary);
      }
    }
  }

  .price-label {
    font-size: 0.65rem;
    color: var(--pico-muted-color);
    text-align: center;
  }

  .price-value {
    font-size: 0.75rem;
    font-weight: 600;
    text-align: center;
    display: flex;
    align-items: center;
    gap: 0.2rem;
  }

  .indicator {
    font-size: 0.85rem;
  }

  .price-input {
    width: 50%;
    padding: 0.15rem 0.3rem;
    font-size: 0.7rem;
    margin: 0 !important;
    height: auto;
    text-align: right;
  }

  input[type='number']::-webkit-inner-spin-button,
  input[type='number']::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  input[type='number'] {
    -moz-appearance: textfield;
  }
}
</style>