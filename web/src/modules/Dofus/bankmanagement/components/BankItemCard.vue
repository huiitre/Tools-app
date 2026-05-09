<script setup lang="ts">
import { computed } from 'vue';
import type { BankItem } from '../domain/BankItem';
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum';
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector';
import ItemContextTrigger from '@/modules/Dofus/item/components/ItemContextTrigger.vue';
import type { Item } from '@/modules/Dofus/item/types/item.types';
import { formatNumber } from '@/utils/formatNumber';
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices';

const props = defineProps<{
  bankItem: BankItem;
}>();

const { get: getPrice } = useItemPrices();

const imageUrl = computed(() => {
  if (!props.bankItem.metadata) return '';
  const img = getItemImageByResolution(props.bankItem.metadata.images ?? [], AssetResolution.X2);
  return img?.url ?? '';
});

/**
 * Calcul du prix total pour cet emplacement
 */
const totalPrice = computed(() => {
  if (!props.bankItem.metadata) return null;
  const priceData = getPrice(props.bankItem.metadata.id);
  
  if (!priceData) return null;
  
  // On utilise le prix moyen communautaire par défaut
  const unitPrice = priceData.communityAveragePrice || 0;
  return unitPrice * props.bankItem.quantity;
});

const totalPriceDisplay = computed(() => {
  if (totalPrice.value === null) return '???';
  if (totalPrice.value === 0) return '0';
  
  // Formatage court si > 1M pour pas péter l'UI
  if (totalPrice.value >= 1000000) {
    return (totalPrice.value / 1000000).toFixed(1) + 'm';
  }
  
  return formatNumber(totalPrice.value);
});

/**
 * On mappe ItemLight vers Item pour la compatibilité avec le composant de Tooltip
 */
const itemForTrigger = computed<Item | undefined>(() => {
  const meta = props.bankItem.metadata;
  if (!meta) return undefined;
  
  return {
    ...meta,
    type: meta.itemType,
    description: '',
    hasRecipe: false,
    farmZones: []
  } as Item;
});

const quantityDisplay = computed(() => {
  return formatNumber(props.bankItem.quantity);
});
</script>

<template>
  <ItemContextTrigger 
    v-if="itemForTrigger"
    :item="itemForTrigger"
  >
    <div class="bank-item-card">
      <div class="price-badge" v-if="totalPrice !== 0 || totalPrice === null">
        {{ totalPriceDisplay }}
      </div>
      
      <div class="image-wrapper">
        <img :src="imageUrl" :alt="bankItem.metadata?.name" loading="lazy" />
      </div>
      
      <div class="quantity-badge">
        {{ quantityDisplay }}
      </div>
    </div>
  </ItemContextTrigger>
  
  <div v-else class="bank-item-card loading" title="Chargement des métadonnées...">
    <div class="image-wrapper">
       <span class="mdi mdi-sync fa-spin"></span>
    </div>
    <div class="quantity-badge">{{ quantityDisplay }}</div>
  </div>
</template>

<style scoped lang="scss">
.bank-item-card {
  position: relative;
  width: 64px;
  height: 64px;
  background-color: var(--pico-card-sectioning-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.15s ease-in-out;
  display: flex;
  align-items: center;
  justify-content: center;

  &:hover {
    border-color: var(--pico-primary);
    background-color: color-mix(in srgb, var(--pico-primary) 10%, var(--pico-card-sectioning-background-color));
    transform: translateY(-2px);
    z-index: 10;
    
    .price-badge { background-color: var(--pico-primary); color: white; }
  }

  &.loading {
    opacity: 0.6;
    cursor: wait;
    .mdi { font-size: 1.5rem; opacity: 0.4; }
  }

  .price-badge {
    position: absolute;
    top: 2px;
    left: 0;
    right: 0;
    text-align: center;
    font-size: 0.55rem;
    font-weight: 700;
    color: var(--pico-muted-color);
    background: rgba(0,0,0,0.05);
    padding: 1px 0;
    border-radius: 2px;
    pointer-events: none;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    transition: all 0.15s ease;
  }

  .image-wrapper {
    width: 42px;
    height: 42px;
    margin-top: 6px; // Pour laisser la place au prix
    display: flex;
    align-items: center;
    justify-content: center;

    img {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
    }
  }

  .quantity-badge {
    position: absolute;
    bottom: 2px;
    right: 4px;
    font-size: 0.65rem;
    font-weight: 800;
    color: white;
    text-shadow: 
      1px 1px 0 #000,
      -1px -1px 0 #000,
      1px -1px 0 #000,
      -1px 1px 0 #000;
    pointer-events: none;
  }
}

.fa-spin {
  animation: fa-spin 2s infinite linear;
}

@keyframes fa-spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(359deg); }
}
</style>
