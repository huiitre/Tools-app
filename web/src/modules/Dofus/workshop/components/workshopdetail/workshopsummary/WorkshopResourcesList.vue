<script setup lang="ts">
import { computed } from 'vue'
import { Item } from '@/modules/Dofus/item/types/item.types'
import WorkshopResourceItem from './WorkshopResourceItem.vue'
import type { WorkshopSortBy, WorkshopSortOrder } from '../../../store/workshopDetail.store'

interface Resource {
  id: number
  name: string
  icon: string
  item: Item
  price: number
  qty: number
  max: number
}

interface Zone {
  name: string
  percent: number
  resources: Resource[]
}

interface Props {
  zones: Zone[]
  showCompleted: boolean
  condensed: boolean
  displayByZone: boolean
  sortBy: WorkshopSortBy
  sortOrder: WorkshopSortOrder
}

interface Emits {
  (e: 'update:resourceQty', resourceId: number, value: number): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const flatResources = computed(() => {
  const resources: Resource[] = []
  const seen = new Set<number>()

  for (const zone of props.zones) {
    for (const res of zone.resources) {
      if (!seen.has(res.id)) {
        resources.push(res)
        seen.add(res.id)
      }
    }
  }

  return sortResources(resources)
})

function sortResources(resources: Resource[]) {
    return [...resources].sort((a, b) => {
        let result = 0;
        if (props.sortBy === 'quantity') {
            result = a.max - b.max;
        } else if (props.sortBy === 'unitPrice') {
            result = (a.price || 0) - (b.price || 0);
        } else if (props.sortBy === 'totalPrice') {
            result = ((a.price || 0) * a.max) - ((b.price || 0) * b.max);
        } else if (props.sortBy === 'remainingPrice') {
            result = ((a.price || 0) * (a.max - a.qty)) - ((b.price || 0) * (b.max - b.qty));
        } else {
            result = a.name.localeCompare(b.name);
        }
        
        if (result === 0) {
            result = a.name.localeCompare(b.name);
        }

        return props.sortOrder === 'asc' ? result : -result;
    });
}

function updateResourceQty(itemId: number, value: number) {
  emit('update:resourceQty', itemId, value)
}
</script>

<template>
  <div class="zones-container" :class="{ 'is-flat': !displayByZone }">
    <!-- DISPLAY BY ZONE -->
    <template v-if="displayByZone">
      <template v-for="zone in zones" :key="zone.name">
        <div class="zone-header">
          <span>{{ zone.name }}</span>
          <span :class="{ ok: zone.percent === 100, warn: zone.percent === 0 }">
            {{ zone.percent }}%
          </span>
        </div>

        <WorkshopResourceItem
          v-for="res in zone.resources"
          :key="res.id"
          class="zone-resource"
          :resource="res"
          :condensed="condensed"
          v-show="showCompleted || res.qty < res.max"
          @update:qty="updateResourceQty(res.id, $event)"
        />
      </template>
    </template>

    <!-- FLAT DISPLAY -->
    <template v-else>
      <WorkshopResourceItem
        v-for="res in flatResources"
        :key="res.id"
        class="zone-resource"
        :resource="res"
        :condensed="condensed"
        v-show="showCompleted || res.qty < res.max"
        @update:qty="updateResourceQty(res.id, $event)"
      />
    </template>
  </div>
</template>

<style scoped>
.zones-container {
  column-width: 380px;
  column-gap: 1rem;
}

.zones-container.is-flat {
  column-width: 380px;
}

.zone-header {
  break-inside: avoid;
  break-after: avoid-column;
  -webkit-column-break-inside: avoid;
  -webkit-column-break-after: avoid;

  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.3rem 0;
  border-bottom: 1px solid var(--pico-muted-border-color);
  font-weight: 600;
  font-size: 0.85rem;
}

.zone-header:not(:first-child) {
  margin-top: 1.5rem;
}

.zone-header .ok {
  color: var(--pico-ins-color);
}

.zone-header .warn {
  color: var(--pico-del-color);
}

.zone-resource {
  break-inside: avoid;
  -webkit-column-break-inside: avoid;
}
</style>
