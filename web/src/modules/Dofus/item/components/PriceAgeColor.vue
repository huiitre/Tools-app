<script setup lang="ts">
import { PriceAgeStatus } from '@/modules/Dofus/item/utils/itemPriceSelector'

defineProps<{ status: PriceAgeStatus | null }>()
</script>

<template>
  <span :class="status && status !== PriceAgeStatus.FRESH ? `price-age--${status}` : ''"><slot /></span>
</template>

<style scoped>
.price-age--warning {
  color: var(--pico-color-orange-500, #ff9800);
}

.price-age--danger {
  color: var(--pico-color-red-500, #f44336);
}
</style>
