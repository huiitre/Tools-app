<script setup lang="ts">
import WorkshopItemCardHeader from './WorkshopItemCardHeader.vue'
import WorkshopItemCardFooter from './WorkshopItemCardFooter.vue'
import WorkshopItemCardPrices from './WorkshopItemCardPrices.vue'
import WorkshopItemCardIngredients from './WorkshopItemCardIngredients.vue'
import WorkshopItemCardCraftBanner from './WorkshopItemCardCraftBanner.vue'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'

export type CraftCard = {
  type: 'main' | 'craft'
  workshopItem: WorkshopItem
  craftedIngredient?: WorkshopItemIngredient
  subIngredients?: WorkshopItemIngredient[]
  level?: number
  parentName?: string
}

const props = defineProps<{
  card: CraftCard
}>()
</script>

<template>
  <article class="item-card">
    <!-- CRAFT BANNER -->
    <WorkshopItemCardCraftBanner
      v-if="props.card.type === 'craft'"
      :level="props.card.level!"
      :parentName="props.card.parentName"
    />

    <!-- HEADER -->
    <WorkshopItemCardHeader :card="props.card" />

    <!-- PRICES -->
    <WorkshopItemCardPrices :card="props.card" />

    <!-- INGREDIENTS -->
    <WorkshopItemCardIngredients :card="props.card" />

    <!-- FOOTER -->
    <WorkshopItemCardFooter :card="props.card" />
  </article>
</template>

<style scoped>
.item-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  overflow: hidden;
}
</style>
