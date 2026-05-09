<script setup lang="ts">
import { computed } from 'vue'
import { ItemLight } from '@/modules/Dofus/item/types/item.types'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { formatNumber } from '@/utils/formatNumber'

const props = defineProps<{
  item?: ItemLight
  itemId: number
  captureId: string
  instances: any[]
  averagePrice: number
  isExpanded: boolean
}>()

const emit = defineEmits(['toggle'])

const imageUrl = computed(() => {
  if (!props.item) return ''
  const img = getItemImageByResolution(props.item.images ?? [], AssetResolution.X2)
  return img?.url ?? ''
})

const sortedInstances = computed(() => {
  return [...props.instances].sort((a, b) => {
    if (a.p1 !== b.p1) return (a.p1 || Infinity) - (b.p1 || Infinity);
    if (a.p10 !== b.p10) return (a.p10 || Infinity) - (b.p10 || Infinity);
    return (a.p100 || Infinity) - (b.p100 || Infinity);
  });
})
</script>

<template>
  <div class="sniffer-group" :class="{ 'is-expanded': isExpanded }">
    <!-- SUMMARY ROW -->
    <div class="sniffer-row summary" @click="emit('toggle')">
      <!-- IMAGE -->
      <div class="cell image">
        <img v-if="imageUrl" :src="imageUrl" :alt="item?.name" />
        <div v-else class="image-placeholder">
          <span class="mdi mdi-image-off-outline"></span>
        </div>
      </div>

      <!-- METADATA -->
      <div class="cell metadata">
        <div v-if="item" class="item-details">
          <span class="type-tag">{{ item.itemType.name }}</span>
          <span class="level-tag">Lvl {{ item.level }}</span>
          <span class="item-name">{{ item.name }}</span>
        </div>
        <div v-else class="item-details loading">
          <span class="muted">Chargement de l'item #{{ itemId }}...</span>
        </div>
      </div>

      <!-- AVERAGE -->
      <div class="cell average">
        <div class="price-group">
          <span class="price-label">Moyenne</span>
          <span class="price-value featured">{{ formatNumber(averagePrice) }} <small>k</small></span>
        </div>
      </div>

      <!-- CHEVRON -->
      <div class="cell expand-icon">
        <span class="mdi" :class="isExpanded ? 'mdi-chevron-up' : 'mdi-chevron-down'"></span>
      </div>
    </div>

    <!-- DETAILS (ACCORDION) -->
    <div v-if="isExpanded" class="sniffer-details">
      <div 
        v-for="(ins, idx) in sortedInstances" 
        :key="ins.instanceId" 
        class="detail-row"
      >
        <div class="cell rank">
          <span class="rank-number">{{ idx + 1 }}</span>
        </div>
        <div class="cell prices-list">
          <div class="detail-price">
            <span class="label">x1</span>
            <span class="value">{{ formatNumber(ins.p1) }} <small>k</small></span>
          </div>
          <div class="detail-price">
            <span class="label">x10</span>
            <span class="value">{{ formatNumber(ins.p10) }} <small>k</small></span>
          </div>
          <div class="detail-price">
            <span class="label">x100</span>
            <span class="value">{{ formatNumber(ins.p100) }} <small>k</small></span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.sniffer-group {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.5rem;
  overflow: hidden;
  transition: all 0.2s ease-in-out;

  &.is-expanded {
    border-color: var(--pico-primary-border);
    box-shadow: var(--pico-card-box-shadow);
  }
}

.sniffer-row.summary {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem 1rem;
  cursor: pointer;
  user-select: none;

  &:hover {
    background: var(--pico-card-sectioning-background-color);
  }
}

.cell {
  display: flex;
  align-items: center;
  min-width: 0;
}

.image {
  flex-shrink: 0;
  img { width: 32px; height: 32px; object-fit: contain; }
  .image-placeholder {
    width: 32px; height: 32px;
    display: flex; align-items: center; justify-content: center;
    background: var(--pico-card-sectioning-background-color);
    border-radius: 0.25rem; color: var(--pico-muted-color);
  }
}

.metadata { flex-grow: 1; }

.item-details {
  display: flex; align-items: center; gap: 0.75rem;
  white-space: nowrap; overflow: hidden;
  .type-tag { font-size: 0.75rem; padding: 0.1rem 0.4rem; background: var(--pico-card-sectioning-background-color); border-radius: 0.25rem; color: var(--pico-muted-color); }
  .level-tag { font-size: 0.75rem; color: var(--pico-muted-color); font-weight: 500; }
  .item-name { font-weight: 600; text-overflow: ellipsis; overflow: hidden; }
}

.average {
  min-width: 120px;
  justify-content: flex-end;
  .price-group {
    display: flex; flex-direction: column; align-items: flex-end;
    .price-label { font-size: 0.7rem; text-transform: uppercase; color: var(--pico-muted-color); margin-bottom: 0.1rem; }
    .price-value.featured { color: var(--pico-primary); font-weight: bold; font-size: 1.1rem; }
  }
}

.expand-icon {
  color: var(--pico-muted-color);
  font-size: 1.5rem;
}

.sniffer-details {
  border-top: 1px solid var(--pico-muted-border-color);
  background: var(--pico-card-sectioning-background-color);
  padding: 0.5rem 0;

  .detail-row {
    display: flex;
    align-items: center;
    padding: 0.4rem 1.5rem;
    gap: 1.5rem;
    border-bottom: 1px solid rgba(var(--pico-muted-border-color), 0.3);

    &:last-child { border-bottom: none; }

    .rank {
      min-width: 24px;
      justify-content: center;
      .rank-number {
        font-family: monospace;
        font-size: 0.8rem;
        color: var(--pico-muted-color);
        background: var(--pico-background-color);
        padding: 0.1rem 0.3rem;
        border-radius: 0.2rem;
      }
    }

    .prices-list {
      flex-grow: 1;
      display: flex;
      justify-content: space-around;
      
      .detail-price {
        display: flex;
        gap: 0.5rem;
        align-items: baseline;
        .label { font-size: 0.75rem; color: var(--pico-muted-color); font-weight: 600; }
        .value { font-weight: 500; color: var(--pico-contrast); }
      }
    }
  }
}

.muted { color: var(--pico-muted-color); }
</style>
