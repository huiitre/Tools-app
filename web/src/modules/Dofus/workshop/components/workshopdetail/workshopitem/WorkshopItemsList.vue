<script setup lang="ts">
import { computed } from 'vue'
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import type { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import WorkshopItemCard from '@/modules/Dofus/workshop/components/workshopdetail/workshopitem/WorkshopItemCard.vue'

const store = useWorkshopDetailStore()
const items = computed(() => store.items)

type CraftCard = {
  type: 'main' | 'craft'
  workshopItem: WorkshopItem
  craftedIngredient?: WorkshopItemIngredient
  subIngredients?: WorkshopItemIngredient[]
  level?: number
  parentName?: string
}

const cardGroups = computed(() => {
  const groups: CraftCard[][] = []

  for (const workshopItem of items.value) {
    const group: CraftCard[] = []

    group.push({
      type: 'main',
      workshopItem
    })

    const craftableIngredients = findCraftableIngredientsRecursive(workshopItem, workshopItem.ingredients)
    craftableIngredients.sort((a, b) => (a.level || 0) - (b.level || 0))

    for (const craft of craftableIngredients) group.push(craft)

    groups.push(group)
  }

  return groups
})

function findCraftableIngredientsRecursive(
  workshopItem: WorkshopItem,
  allIngredients: WorkshopItemIngredient[],
  parentId: number | null = null,
  level: number = 1,
  parentName: string = ''
): CraftCard[] {
  const result: CraftCard[] = []

  const ingredients = allIngredients.filter(ing => ing.parentIngredientId === parentId)

  for (const ingredient of ingredients) {
    const subIngredients = allIngredients.filter(ing => ing.parentIngredientId === ingredient.id)

    if (ingredient.item.hasRecipe && subIngredients.length > 0) {
      result.push({
        type: 'craft',
        workshopItem,
        craftedIngredient: ingredient,
        subIngredients,
        level,
        parentName: parentName || workshopItem.item.name
      })

      const deeperCrafts = findCraftableIngredientsRecursive(
        workshopItem,
        allIngredients,
        ingredient.id,
        level + 1,
        ingredient.item.name
      )

      result.push(...deeperCrafts)
    }
  }

  return result
}
</script>

<template>
  <div class="workshop-items-container">
    <template v-for="(group, groupIndex) in cardGroups" :key="groupIndex">

      <!-- GROUPE AVEC CRAFTS -->
      <div v-if="group.length > 1" class="item-group">
        <WorkshopItemCard
          v-for="(card, cardIndex) in group"
          :key="cardIndex"
          :card="card"
        />
      </div>

      <!-- ITEM SANS CRAFT -->
      <WorkshopItemCard v-else :card="group[0]" />

    </template>
  </div>
</template>

<style scoped>
/* CONTAINER */
.workshop-items-container {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1rem;
}

/* GROUP (prend toute la largeur) */
.item-group {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1rem;
  border: 4px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  padding: 1rem;
}
</style>
