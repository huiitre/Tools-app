<script setup lang="ts">
import { computed } from 'vue'
import { useImagePreview } from '@/composables/useImagePreview'
import type { ValorantNightMarket } from '../valorant.types'
import ValorantSkinActions from './ValorantSkinActions.vue'

const props = defineProps<{
  market: ValorantNightMarket
  now: number
}>()

const { open: openImagePreview } = useImagePreview()

const formattedTime = computed(() => {
  const ms = Math.max(0, props.market.expiresAt - props.now)
  const d = Math.floor(ms / 86_400_000)
  const h = Math.floor((ms % 86_400_000) / 3_600_000)
  const m = Math.floor((ms % 3_600_000) / 60_000)
  return d > 0 ? `${d}j ${h}h ${m}min` : `${h}h ${m}min`
})
</script>

<template>
  <div class="night-market">
    <div class="market-header">
      <div class="header-left">
        <i class="mdi mdi-moon-waning-crescent" />
        <span class="market-title">Marché Noir</span>
      </div>
      <div class="header-right">
        <span class="market-timer">Expire dans <strong>{{ formattedTime }}</strong></span>
      </div>
    </div>

    <div class="market-grid">
      <article
        v-for="(offer, i) in market.offers"
        :key="offer.id"
        class="market-card"
        :style="{ '--delay': `${i * 100}ms` }"
      >
        <div
          class="skin-image-wrap"
          @click="offer.iconUrl && openImagePreview(offer.iconUrl, offer.name)"
        >
          <img
            v-if="offer.iconUrl"
            :src="offer.iconUrl"
            :alt="offer.name"
            class="skin-image"
            loading="lazy"
          />
          <ValorantSkinActions :skin-id="offer.id" class="card-actions" />
          <div class="discount-badge">-{{ offer.discountPercent }}%</div>
        </div>

        <div class="skin-info">
          <div class="skin-name">{{ offer.name }}</div>
          <div class="price-row">
            <span class="vp-badge">{{ offer.discountedCost.toLocaleString('fr-FR') }} VP</span>
            <span class="original-price">{{ offer.baseCost.toLocaleString('fr-FR') }} VP</span>
          </div>
        </div>
      </article>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.night-market {
  margin-top: 3.5rem;
  margin-bottom: 2rem;
  animation: fade-in 0.6s ease both;
}

@keyframes fade-in {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

.market-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
  padding: 0 0.5rem;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: #f1c40f;

  i { font-size: 1.4rem; }
}

.market-title {
  font-size: 1.25rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--pico-color);
}

.market-timer {
  font-size: 0.85rem;
  color: var(--pico-muted-color);

  strong {
    color: var(--pico-primary);
    font-variant-numeric: tabular-nums;
  }
}

.market-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1.25rem;
}

.market-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  transition: transform 0.28s ease, box-shadow 0.28s ease, border-color 0.28s ease;
  animation: card-enter 0.5s cubic-bezier(0.22, 1, 0.36, 1) both;
  animation-delay: var(--delay, 0ms);

  &:hover {
    transform: translateY(-6px);
    border-color: color-mix(in srgb, var(--pico-primary) 45%, transparent);
    box-shadow:
      0 16px 40px rgba(0, 0, 0, 0.22),
      0 0 0 1px color-mix(in srgb, var(--pico-primary) 18%, transparent);

    .skin-image { transform: scale(1.08); }
    :deep(.action-btn) { opacity: 1; }
  }
}

@keyframes card-enter {
  from { opacity: 0; transform: scale(0.92); }
  to { opacity: 1; transform: scale(1); }
}

.skin-image-wrap {
  position: relative;
  background: #0d0d1a;
  height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 1.25rem;
  cursor: pointer;
}

.skin-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  transition: transform 0.3s ease;
}

.discount-badge {
  position: absolute;
  bottom: 0.6rem;
  left: 0.6rem;
  font-size: 0.78rem;
  font-weight: 700;
  color: #22c55e;
  background: color-mix(in srgb, #22c55e 12%, transparent);
  border: 1px solid color-mix(in srgb, #22c55e 28%, transparent);
  padding: 0.15rem 0.45rem;
  border-radius: 999px;
}

.card-actions {
  position: absolute;
  top: 0.6rem;
  right: 0.6rem;

  :deep(.action-btn) {
    opacity: 0;
    transition: opacity 0.2s ease;
    &.active { opacity: 1; }
  }
}

.skin-info {
  padding: 0.85rem 1rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.skin-name {
  font-weight: 600;
  font-size: 0.85rem;
  color: var(--pico-color);
  line-height: 1.2;
}

.price-row {
  margin-top: auto;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.original-price {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  text-decoration: line-through;
}

.vp-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in srgb, #ff4655 10%, transparent);
  border: 1px solid color-mix(in srgb, #ff4655 25%, transparent);
  font-size: 0.78rem;
  font-weight: 700;
  color: #ff4655;
}
</style>
