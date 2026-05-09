<script setup lang="ts">
import { useWorkshopDetailStore } from '@/modules/Dofus/workshop/store/workshopDetail.store';
import { CraftCard } from '@/modules/Dofus/workshop/types/workshop.types';
import { storeToRefs } from 'pinia';

const props = defineProps<{
  card: CraftCard
}>()

const store = useWorkshopDetailStore()
const { isOwner } = storeToRefs(store)

async function handleDelete() {
  if (props.card.type === 'main') {
    await store.deleteWorkshopItem(props.card.workshopItem.id)
  } else {
    await store.uncraftIngredient(props.card.craftedIngredient!.id)
  }
}

async function handleUpdateMinQuantityObtained() {
  let ingredients: {
    id: number
    quantityObtained: number
  }[] = []

  if (props.card.type === 'main') {
    ingredients = props.card.workshopItem.ingredients
      .filter(ing => ing.parentIngredientId === null)
  } else {
    ingredients = props.card.subIngredients ?? []
  }

  if (ingredients.length === 0) return

  const payload = ingredients
    .filter(ing => ing.quantityObtained !== 0)
    .map(ing => ({
      id: ing.id,
      quantity: 0
    }))

  if (payload.length === 0) return

  await store.setIngredientsQuantityObtained(payload)
}

async function handleUpdateMaxQuantityObtained() {
  let ingredients: { id: number; quantityRequired: number; quantityObtained: number }[] = []

  if (props.card.type === 'main') {
    ingredients = props.card.workshopItem.ingredients
      .filter(ing => ing.parentIngredientId === null)
  } else {
    ingredients = props.card.subIngredients ?? []
  }

  console.log("%c WorkshopItemCardFooter.vue #58 || ingredients : ", 'background:red;color:#fff;font-weight:bold;', ingredients);

  if (ingredients.length === 0) return

  const payload = ingredients
    .filter(ing => ing.quantityObtained !== ing.quantityRequired)
    .map(ing => ({
      id: ing.id,
      quantity: ing.quantityRequired
    }))

  if (payload.length === 0) return

  await store.setIngredientsQuantityObtained(payload)
}

</script>

<template>
  <footer class="item-footer" v-if="isOwner">
    <button class="footer-btn" @click="handleUpdateMinQuantityObtained">↓ 0</button>
    <button class="footer-btn" @click="handleUpdateMaxQuantityObtained">↑ max</button>
    <button class="footer-btn danger" @click="handleDelete">
      <i class="mdi mdi-delete"></i>
    </button>
  </footer>
</template>

<style scoped>
.item-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.25rem;
  padding-top: 0.25rem;
  border-top: 1px solid var(--pico-muted-border-color);
  margin-top: auto;
}

.footer-btn {
  padding: 0.15rem 0.35rem;
  font-size: 0.65rem;
  line-height: 1;
  background: var(--pico-form-element-background-color);
  border: 1px solid var(--pico-form-element-border-color);
  color: var(--pico-form-element-color);
  border-radius: var(--pico-border-radius);
  cursor: pointer;
  margin: 0 !important;
  transition: all 0.15s;
}

.footer-btn:hover {
  background: var(--pico-primary-background);
  border-color: var(--pico-primary-border);
  color: var(--pico-primary-inverse);
}

.footer-btn.danger {
  background: transparent;
  color: var(--pico-del-color);
  border-color: var(--pico-muted-border-color);
}

.footer-btn.danger:hover {
  background: var(--pico-del-color);
  border-color: var(--pico-del-color);
  color: white;
}
</style>
