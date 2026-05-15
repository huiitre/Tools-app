<script setup lang="ts">
import { computed } from 'vue'
import { useImagePreview } from '@/composables/useImagePreview'
import { useValorantSkinDetail } from '../composables/useValorantSkinDetail'
import type { ValorantStoreBundle } from '../valorant.types'
import ValorantSkinActions from './ValorantSkinActions.vue'
import ValorantSkinIdBadge from './ValorantSkinIdBadge.vue'

const props = defineProps<{
  bundle: ValorantStoreBundle
  now: number
}>()

const { open: openImagePreview } = useImagePreview()
const { open: openSkinDetail } = useValorantSkinDetail()

const formattedTime = computed(() => {
  const ms = Math.max(0, props.bundle.remainingSeconds * 1000)
  const d = Math.floor(ms / 86_400_000)
  const h = Math.floor((ms % 86_400_000) / 3_600_000)
  const m = Math.floor((ms % 3_600_000) / 60_000)
  return d > 0 ? `${d}j ${h}h ${m}min` : `${h}h ${m}min`
})
</script>

<template>
  <article class="bundle-card">
    <div
      class="bundle-banner"
      @click="props.bundle.bannerUrl && openImagePreview(props.bundle.bannerUrl, props.bundle.name)"
    >
      <img v-if="props.bundle.bannerUrl" :src="props.bundle.bannerUrl" :alt="props.bundle.name" loading="lazy" />
      <div v-else class="bundle-banner-placeholder" />
    </div>

    <div class="bundle-info-row">
      <div class="bundle-info-left">
        <div class="bundle-name">{{ props.bundle.name }}</div>
        <div class="bundle-price-row">
          <span class="vp-badge">{{ props.bundle.totalDiscountedCost.toLocaleString('fr-FR') }} VP</span>
          <template v-if="props.bundle.discountPercent > 0">
            <span class="bundle-original">{{ props.bundle.totalBaseCost.toLocaleString('fr-FR') }} VP</span>
            <span class="bundle-discount-badge">-{{ props.bundle.discountPercent }}%</span>
          </template>
        </div>
      </div>
      <div class="bundle-timer-block">
        <div class="bundle-countdown">{{ formattedTime }}</div>
        <div class="bundle-timer-label">restant</div>
      </div>
    </div>

    <div v-if="props.bundle.items.length" class="bundle-skins-grid">
      <div v-for="offer in props.bundle.items" :key="offer.skin.id" class="bundle-skin-item">
        <div
          class="bundle-skin-img"
          @click="openSkinDetail(offer.skin)"
        >
          <ValorantSkinIdBadge :id="offer.skin.id" />
          <img v-if="offer.skin.iconUrl" :src="offer.skin.iconUrl" :alt="offer.skin.name" loading="lazy" />
          <ValorantSkinActions :skin-id="offer.skin.id" class="skin-actions" />
        </div>
        <span class="bundle-skin-label">{{ offer.skin.name }}</span>
        <div class="bundle-skin-price">
          <span v-if="offer.cost > 0" class="vp-badge">{{ offer.cost.toLocaleString('fr-FR') }} VP</span>
          <span v-else class="vp-badge vp-badge--free">OFFERT</span>
        </div>
      </div>
    </div>
  </article>
</template>

<style lang="scss" scoped>
@keyframes card-enter {
  from { opacity: 0; transform: translateY(24px); }
  to   { opacity: 1; transform: translateY(0); }
}

.bundle-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
  margin-bottom: 1.25rem;
  animation: card-enter 0.45s cubic-bezier(0.22, 1, 0.36, 1) both;
  animation-delay: 80ms;
}

.bundle-banner {
  width: 100%;
  background: #0d0d1a;
  overflow: hidden;
  cursor: pointer;

  img {
    width: 100%;
    height: auto;
    object-fit: contain;
    display: block;
    transition: transform 0.32s ease;
  }

  &:hover img { transform: scale(1.02); }
}

.bundle-banner-placeholder {
  width: 100%;
  height: 180px;
}

.bundle-info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);

  @media (max-width: 520px) {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.6rem;
  }
}

.bundle-info-left {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.bundle-name {
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--pico-color);
  line-height: 1.3;
}

.bundle-price-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.bundle-original {
  font-size: 0.82rem;
  color: var(--pico-muted-color);
  text-decoration: line-through;
}

.bundle-discount-badge {
  font-size: 0.78rem;
  font-weight: 700;
  color: #22c55e;
  background: color-mix(in srgb, #22c55e 12%, transparent);
  border: 1px solid color-mix(in srgb, #22c55e 28%, transparent);
  padding: 0.15rem 0.45rem;
  border-radius: 999px;
}

.bundle-timer-block {
  text-align: right;
  flex-shrink: 0;

  @media (max-width: 520px) { text-align: left; }
}

.bundle-countdown {
  font-size: 1.25rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: var(--pico-primary);
  line-height: 1.2;
}

.bundle-timer-label {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  margin-top: 0.1rem;
}

.bundle-skins-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 0;
  border-top: 1px solid var(--pico-card-border-color);
}

.bundle-skin-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  padding: 0.85rem 0.75rem;
  border-right: 1px solid var(--pico-card-border-color);
  border-bottom: 1px solid var(--pico-card-border-color);

  &:last-child { border-right: none; }
}

.bundle-skin-img {
  position: relative;
  width: 100%;
  height: 72px;
  background: #0d0d1a;
  border-radius: 8px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;

  img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
    display: block;
    transition: transform 0.28s ease;
  }

  &:hover img { transform: scale(1.1); }

  &:hover {
    :deep(.action-btn), :deep(.skin-id-badge) { opacity: 1; }
  }
}

.skin-actions {
  position: absolute;
  top: 0.25rem;
  right: 0.25rem;

  :deep(.action-btn) {
    opacity: 0;
    width: 1.3rem;
    height: 1.3rem;
    background: rgba(0, 0, 0, 0.7);

    i { font-size: 0.8rem; }

    &.active { opacity: 1; }
  }
}

.bundle-skin-label {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  text-align: center;
  line-height: 1.3;
}

.bundle-skin-price {
  margin-top: 0.2rem;
}

.vp-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.22rem 0.6rem;
  border-radius: 999px;
  background: color-mix(in srgb, #ff4655 10%, transparent);
  border: 1px solid color-mix(in srgb, #ff4655 28%, transparent);
  font-size: 0.82rem;
  font-weight: 700;
  color: #ff4655;
  letter-spacing: 0.03em;
  white-space: nowrap;

  &.vp-badge--free {
    background: rgba(34, 197, 94, 0.1);
    border-color: rgba(34, 197, 94, 0.3);
    color: #22c55e;
  }
}
</style>
