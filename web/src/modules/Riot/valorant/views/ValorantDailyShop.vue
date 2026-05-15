<script setup lang="ts">
import { useValorantShop, REGIONS } from '../composables/useValorantShop'
import { useValorantSkinDetail } from '../composables/useValorantSkinDetail'
import ValorantAuthCard from '../components/ValorantAuthCard.vue'
import ValorantBundleCard from '../components/ValorantBundleCard.vue'
import ValorantSkinActions from '../components/ValorantSkinActions.vue'
import ValorantNightMarket from '../components/ValorantNightMarket.vue'
import ValorantShopHistoryPopup from '../components/ValorantShopHistoryPopup.vue'
import ValorantSkinIdBadge from '../components/ValorantSkinIdBadge.vue'

const {
  view, skins, bundles, nightMarket, isRenewing, error, bundleNow,
  formattedTime, handleSubmit, reset, currentRegionLabel,
} = useValorantShop()

const { open: openSkinDetail } = useValorantSkinDetail()
</script>

<template>
  <div class="valorant-shop-view">
    <!-- ── FORM ───────────────────────────────────────────────── -->
    <div v-if="view === 'form'" class="form-wrapper">
      <ValorantAuthCard
        :error="error"
        :regions="REGIONS"
        @submit="({ token, region, mode }) => handleSubmit(token, region, mode)"
      />
    </div>

    <!-- ── LOADING (skeleton) ─────────────────────────────────── -->
    <div v-else-if="view === 'loading'" class="shop-wrapper">
      <div class="shop-meta skeleton-meta">
        <div class="skeleton-line" style="width: 160px; height: 0.85rem;" />
      </div>
      <div class="shop-timer skeleton-timer">
        <div class="skeleton-line" style="width: 200px; height: 3rem; border-radius: 8px;" />
      </div>
      <div class="skin-grid">
        <div v-for="i in 4" :key="i" class="skin-card skin-card--skeleton">
          <div class="skeleton-image" />
          <div class="skin-info">
            <div class="skeleton-line" style="width: 72%; height: 1rem;" />
            <div class="skeleton-line" style="width: 38%; height: 0.85rem; margin-top: 0.35rem;" />
          </div>
        </div>
      </div>
    </div>

    <!-- ── SHOP ───────────────────────────────────────────────── -->
    <div v-else class="shop-wrapper">
      <div class="shop-meta">
        <span class="shop-region">
          <i class="mdi mdi-map-marker-outline" />
          {{ currentRegionLabel() }}
        </span>
        <div class="shop-actions">
          <ValorantShopHistoryPopup />
          <button class="reset-btn" @click="reset">
            <i class="mdi mdi-refresh" />
            Changer de token
          </button>
        </div>
      </div>

      <div class="shop-timer" :class="{ 'shop-timer--renewing': isRenewing }">
        {{ formattedTime }}
      </div>
      <Transition name="fade">
        <div v-if="isRenewing" class="renewing-hint">
          <i class="mdi mdi-refresh renewing-spin" />
          Vérification des nouveaux skins...
        </div>
      </Transition>

      <div class="skin-grid">
        <article
          v-for="(offer, i) in skins"
          :key="offer.skin.id"
          class="skin-card"
          :style="{ '--delay': `${i * 110}ms` }"
        >
          <div class="skin-image-wrap" @click="openSkinDetail(offer.skin)">
            <ValorantSkinIdBadge :id="offer.skin.id" />
            <img :src="offer.skin.iconUrl ?? undefined" :alt="offer.skin.name" class="skin-image" loading="lazy" />
            <ValorantSkinActions :skin-id="offer.skin.id" class="card-actions" />
          </div>
          <div class="skin-info">
            <div class="skin-name">{{ offer.skin.name }}</div>
            <div class="skin-price">
              <span class="vp-badge">{{ offer.cost.toLocaleString('fr-FR') }} VP</span>
            </div>
          </div>
        </article>
      </div>

      <template v-if="bundles.length">
        <div class="bundle-section-header">
          <i class="mdi mdi-package-variant-closed" />
          Pack{{ bundles.length > 1 ? 's' : '' }} en vente
        </div>
        <ValorantBundleCard
          v-for="b in bundles"
          :key="b.assetId"
          :bundle="b"
          :now="bundleNow"
        />
      </template>

      <ValorantNightMarket
        v-if="nightMarket"
        :market="nightMarket"
        :now="bundleNow"
      />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.valorant-shop-view {
  width: 100%;
}

.form-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: calc(100vh - 56px - 52px);
  padding: 2rem 1rem;
}

.shop-wrapper {
  padding: 1.5rem;
  max-width: 1200px;
  margin: 0 auto;
}

/* ── Shop meta bar ───────────────────────────────────────────────────────── */
.shop-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
  min-height: 2rem;
}

.skeleton-meta { pointer-events: none; }

.shop-region {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}

.shop-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.reset-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.8rem;
  padding: 0.35rem 0.75rem;
  background: transparent;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 6px;
  color: var(--pico-muted-color);
  cursor: pointer;
  width: auto;
  transition: border-color 0.15s ease, color 0.15s ease;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    background: transparent;
  }
}

/* ── Timer ───────────────────────────────────────────────────────────────── */
.shop-timer {
  text-align: center;
  font-size: 3rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.05em;
  color: var(--pico-primary);
  margin-bottom: 0.75rem;
  transition: opacity 0.3s ease;
}

.skeleton-timer {
  display: flex;
  justify-content: center;
  margin-bottom: 0.75rem;
}

.shop-timer--renewing {
  opacity: 0.35;
  animation: pulse 1.8s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.35; }
  50% { opacity: 0.6; }
}

.renewing-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  margin-bottom: 1.5rem;
}

.renewing-spin { animation: spin 1.2s linear infinite; }

@keyframes spin {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}

.fade-enter-active,
.fade-leave-active { transition: opacity 0.3s ease; }

.fade-enter-from,
.fade-leave-to { opacity: 0; }

/* ── Skin grid ───────────────────────────────────────────────────────────── */
.skin-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 1.25rem;
  margin-top: 0.75rem;
}

.skin-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  transition: transform 0.28s ease, box-shadow 0.28s ease, border-color 0.28s ease;
  animation: card-enter 0.45s cubic-bezier(0.22, 1, 0.36, 1) both;
  animation-delay: var(--delay, 0ms);

  &:hover {
    transform: translateY(-7px);
    border-color: color-mix(in srgb, var(--pico-primary) 45%, transparent);
    box-shadow:
      0 16px 40px rgba(0, 0, 0, 0.22),
      0 0 0 1px color-mix(in srgb, var(--pico-primary) 18%, transparent);

    .skin-image { transform: scale(1.07); }

    :deep(.action-btn), :deep(.skin-id-badge) { opacity: 1; }
  }
}

@keyframes card-enter {
  from { opacity: 0; transform: translateY(24px); }
  to   { opacity: 1; transform: translateY(0); }
}

.skin-image-wrap {
  position: relative;
  background: #0d0d1a;
  height: 210px;
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
  transition: transform 0.32s ease;
}

.skin-info {
  padding: 1rem 1.1rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.skin-name {
  flex: 1;
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--pico-color);
  line-height: 1.3;
}

.skin-price { margin-top: auto; }

.card-actions {
  position: absolute;
  top: 0.6rem;
  right: 0.6rem;

  :deep(.action-btn) {
    opacity: 0;
    transition: opacity 0.2s ease, color 0.15s ease, background 0.15s ease;

    &.active {
      opacity: 1;
    }
  }
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
}

/* ── Skeleton ────────────────────────────────────────────────────────────── */
.skin-card--skeleton {
  animation: none;
  pointer-events: none;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

%shimmer {
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

.skeleton-image {
  @extend %shimmer;
  height: 210px;
}

.skeleton-line {
  @extend %shimmer;
  display: block;
  border-radius: 6px;
}

/* ── Bundle section header ───────────────────────────────────────────────── */
.bundle-section-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 2.5rem;
  margin-bottom: 1rem;
  font-size: 1rem;
  font-weight: 700;
  color: var(--pico-color);

  .mdi {
    font-size: 1.15rem;
    color: var(--pico-primary);
  }
}
</style>
