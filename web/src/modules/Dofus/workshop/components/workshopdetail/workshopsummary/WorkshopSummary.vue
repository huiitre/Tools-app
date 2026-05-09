<script setup lang="ts">
import { ref, computed } from 'vue'
import { useWorkshopSummary } from '@/modules/Dofus/workshop/composables/useWorkshopSummary'
import { useWorkshopPriceCalculator } from '@/modules/Dofus/workshop/composables/useWorkshopPriceCalculator'
import { useWorkshopDetailStore, type WorkshopSortBy, type WorkshopSortOrder } from '@/modules/Dofus/workshop/store/workshopDetail.store'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import WorkshopStats from './WorkshopStats.vue'
import WorkshopPrices from './WorkshopPrices.vue'
import WorkshopFilters from './WorkshopFilters.vue'
import WorkshopResourcesList from './WorkshopResourcesList.vue'

const { summary } = useWorkshopSummary()
const { getUnitPrice } = useWorkshopPriceCalculator()
const workshopDetailStore = useWorkshopDetailStore()

/* ========================= FILTERS ========================= */
const showCompleted = ref(false)

const condensed = computed({
  get: () => workshopDetailStore.condensed,
  set: (value: boolean) => workshopDetailStore.setCondensed(value)
})

const displayByZone = computed({
  get: () => workshopDetailStore.displayByZone,
  set: (value: boolean) => workshopDetailStore.setDisplayByZone(value)
})

const sortBy = computed({
  get: () => workshopDetailStore.sortBy,
  set: (value: WorkshopSortBy) => workshopDetailStore.setSortBy(value)
})

const sortOrder = computed({
  get: () => workshopDetailStore.sortOrder,
  set: (value: WorkshopSortOrder) => workshopDetailStore.setSortOrder(value)
})

/* ========================= COMPUTED DATA ========================= */
const stats = computed(() => ({
  items: summary.value.totalItems,
  totalRes: summary.value.totalResources,
  missing: summary.value.totalMissing,
  complete: summary.value.totalComplete
}))

const prices = computed(() => ({
  buyItems: summary.value.buyItemsCost,
  buyItemsRemaining: summary.value.buyItemsRemaining,
  craft: summary.value.craftCost,
  craftAdjusted: summary.value.craftAdjusted,
  mixedCost: summary.value.mixedCost,
  mixedAdjusted: summary.value.mixedAdjusted
}))

const zones = computed(() => {
  const processedZones = summary.value.zones.map(zone => ({
    name: zone.name,
    percent: zone.percent,
    resources: zone.resources.map(resource => {
      // On récupère le prix de manière réactive via le store
      const unitPrice = getUnitPrice(resource.item.id)
      const img = getItemImageByResolution(resource.item.images ?? [], AssetResolution.X2)

      return {
        id: resource.item.id,
        name: resource.item.name,
        icon: img?.url ?? '',
        item: resource.item,
        price: unitPrice,
        qty: resource.quantityObtained,
        max: resource.quantityRequired
      }
    })
  }))

  return processedZones.map(zone => ({
    ...zone,
    resources: sortResources(zone.resources)
  }))
})

function sortResources(resources: any[]) {
    return [...resources].sort((a, b) => {
        let result = 0;
        if (sortBy.value === 'quantity') {
            result = a.max - b.max;
        } else if (sortBy.value === 'unitPrice') {
            result = (a.price || 0) - (b.price || 0);
        } else if (sortBy.value === 'totalPrice') {
            result = ((a.price || 0) * a.max) - ((b.price || 0) * b.max);
        } else if (sortBy.value === 'remainingPrice') {
            result = ((a.price || 0) * (a.max - a.qty)) - ((b.price || 0) * (b.max - b.qty));
        } else {
            result = a.name.localeCompare(b.name);
        }
        
        if (result === 0) {
            result = a.name.localeCompare(b.name);
        }

        return sortOrder.value === 'asc' ? result : -result;
    });
}

/* ========================= HANDLERS ========================= */
function updateResourceQty(itemId: number, value: number) {
  // TODO: Mutation API
  console.log('Update resource qty', { itemId, value })
}
</script>

<template>
  <aside class="workshop-summary">
    <div class="top-row">
      <WorkshopStats
        :items="stats.items"
        :total-res="stats.totalRes"
        :missing="stats.missing"
        :complete="stats.complete"
      />

      <div class="right-side">
        <WorkshopPrices
          :buy-items="prices.buyItems"
          :buy-items-remaining="prices.buyItemsRemaining"
          :craft="prices.craft"
          :craft-adjusted="prices.craftAdjusted"
          :mixed="prices.mixedCost"
          :mixed-adjusted="prices.mixedAdjusted"
        />

        <WorkshopFilters
          v-model:show-completed="showCompleted"
          v-model:condensed="condensed"
          v-model:display-by-zone="displayByZone"
          v-model:sort-by="sortBy"
          v-model:sort-order="sortOrder"
        />
      </div>
    </div>

    <WorkshopResourcesList
      :zones="zones"
      :show-completed="showCompleted"
      :condensed="condensed"
      :display-by-zone="displayByZone"
      :sort-by="sortBy"
      :sort-order="sortOrder"
      @update:resource-qty="updateResourceQty"
    />
  </aside>
</template>

<style scoped>
.workshop-summary {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  font-size: 0.8rem;
}

.top-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.right-side {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
</style>
