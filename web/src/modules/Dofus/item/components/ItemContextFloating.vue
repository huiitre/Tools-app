<script setup lang="ts">
import { computed } from 'vue'
import type { Item, ItemPrice } from '@/modules/Dofus/item/types/item.types'
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum'
import { formatNumber } from '@/utils/formatNumber'
import { PriceDisplayMode } from '@/modules/Dofus/preferences/types/priceDisplayMode.enum'
import { getItemImageByResolution } from '@/modules/Dofus/item/utils/itemImageSelector'
import { getItemPriceDateByMode, getPriceAgeStatus } from '@/modules/Dofus/item/utils/itemPriceSelector'
import PriceAgeColor from '@/modules/Dofus/item/components/PriceAgeColor.vue'

const props = defineProps<{
  item: Item
  price?: ItemPrice
  visible: boolean
  x: number
  y: number
  priceDisplayMode: PriceDisplayMode
}>()

const primaryZone =
  props.item.farmZones?.find((z: any) => z.primary === true || z.isPrimary === true) ??
  props.item.farmZones?.[0] ??
  null

const otherZones =
  (props.item.farmZones ?? []).filter((z: any) => z !== primaryZone)

const userPriceDate = computed(() => props.price ? getItemPriceDateByMode(props.price, PriceDisplayMode.USER) : null)
const userPriceStatus = computed(() => props.price ? getPriceAgeStatus(props.price, PriceDisplayMode.USER) : null)

const communityPriceDate = computed(() => props.price ? getItemPriceDateByMode(props.price, PriceDisplayMode.COMMUNITY) : null)
const communityPriceStatus = computed(() => props.price ? getPriceAgeStatus(props.price, PriceDisplayMode.COMMUNITY) : null)

const lastPriceDate = computed(() => props.price ? getItemPriceDateByMode(props.price, PriceDisplayMode.LAST) : null)
const lastPriceStatus = computed(() => props.price ? getPriceAgeStatus(props.price, PriceDisplayMode.LAST) : null)
</script>

<template>
  <div
    v-if="visible"
    class="item-context-floating"
    :class="{ compact: !item.farmZones?.length }"
    :style="{ left: x + 'px', top: y + 'px' }"
  >
    <!-- HEADER -->
    <div class="header">
      <img
        v-if="getItemImageByResolution(item.images ?? [], AssetResolution.X2)"
        :src="getItemImageByResolution(item.images ?? [], AssetResolution.X2)!.url"
        class="item-icon"
      />

      <div class="meta">
        <div class="name">{{ item.name }}</div>
        <div class="type">
          {{ item.type.name }} · Niveau {{ item.level }}
        </div>
      </div>
    </div>

    <!-- DESCRIPTION -->
    <div v-if="item.description" class="description">
      {{ item.description }}
    </div>

    <!-- PRICES -->
    <div v-if="price" class="section">
      <div class="section-title">Prix</div>

      <div class="price-line" :class="{ primary: priceDisplayMode === PriceDisplayMode.USER }">
        <span>👤 Mon prix</span>
        <div class="price-value">
          <strong>{{ formatNumber(price.userPrice) }} ₭</strong>
          <PriceAgeColor v-if="userPriceDate" :status="userPriceStatus" class="price-age">{{ userPriceDate }}</PriceAgeColor>
        </div>
      </div>

      <div class="price-line" :class="{ primary: priceDisplayMode === PriceDisplayMode.COMMUNITY }">
        <span>🌍 Prix communautaire</span>
        <div class="price-value">
          <strong>{{ formatNumber(price.communityAveragePrice) }} ₭</strong>
          <PriceAgeColor v-if="communityPriceDate" :status="communityPriceStatus" class="price-age">{{ communityPriceDate }}</PriceAgeColor>
        </div>
      </div>

      <div class="price-line" :class="{ primary: priceDisplayMode === PriceDisplayMode.LAST }">
        <span>🕒 Dernier prix</span>
        <div class="price-value">
          <strong>{{ formatNumber(price.lastUpdatedPrice) }} ₭</strong>
          <PriceAgeColor v-if="lastPriceDate" :status="lastPriceStatus" class="price-age">{{ lastPriceDate }}</PriceAgeColor>
        </div>
      </div>

      <div class="price-separator" />

      <div class="price-line">
        <span>👤 Craft – mon prix</span>
        <strong>{{ formatNumber(price.craftUserPrice) }} ₭</strong>
      </div>

      <div class="price-line">
        <span>🌍 Craft – communautaire</span>
        <strong>{{ formatNumber(price.craftCommunityPrice) }} ₭</strong>
      </div>

      <div class="price-line">
        <span>🕒 Craft – dernier</span>
        <strong>{{ formatNumber(price.craftLastPrice) }} ₭</strong>
      </div>

      <div class="price-line strong">
        <span>🧮 Craft – calculé</span>
        <strong>{{ formatNumber(price.craftCalculatedPrice) }} ₭</strong>
      </div>
    </div>

    <!-- DROP (PRIMARY ONLY) -->
    <div v-if="primaryZone" class="section">
      <div class="section-title">Zones de drop</div>

      <div class="zone">
        <div class="zone-title">
          {{ primaryZone.area.name }} — {{ primaryZone.subarea.name }}
        </div>

        <div class="monsters">
          <div
            v-for="monster in primaryZone.monsters"
            :key="monster.id"
            class="monster"
          >
            <img
              v-if="monster.images?.length"
              :src="monster.images.find(i => i.resolution === AssetResolution.X2)?.url"
              class="monster-icon"
            />
            <span class="monster-name">{{ monster.name }}</span>
          </div>
        </div>
      </div>

      <div v-if="otherZones.length" class="other-zones">
        <span class="other-zones-label">Trouvable également :</span>
        <div class="other-zones-list">
          <span
            v-for="zone in otherZones"
            :key="zone.subarea.id"
            class="other-zone"
          >
            {{ zone.area.name }} — {{ zone.subarea.name }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.item-context-floating {
  position: fixed;
  z-index: 1000;
  pointer-events: none;

  width: 360px;

  background: var(--pico-card-background-color);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 30%, transparent);
  box-shadow: var(--pico-card-box-shadow);
  border-radius: 10px;

  padding: 0.75rem;
  font-size: 0.8rem;
  color: var(--pico-color);
}

.item-context-floating.compact {
  width: 300px;
}

/* HEADER */
.header {
  display: flex;
  gap: 0.6rem;
  align-items: center;
}

.item-icon {
  width: 48px;
  height: 48px;
}

.meta .name {
  font-weight: 600;
}

.meta .type {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

/* DESCRIPTION */
.description {
  margin: 0.5rem 0 0.6rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

/* SECTIONS */
.section {
  margin-top: 0.7rem;
}

.section-title {
  font-weight: 600;
  font-size: 0.75rem;
  margin-bottom: 0.4rem;
  color: var(--pico-primary);
}

/* PRICES */
.price-line {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 0.2rem 0;
}

.price-line.primary {
  color: var(--pico-primary);
}

.price-line.strong {
  font-weight: 600;
}

.price-value {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.price-age {
  font-size: 0.65rem;
  font-style: italic;
  margin-top: 0.1rem;
  font-weight: bold;
}

.price-separator {
  height: 1px;
  margin: 0.3rem 0;
  background: var(--pico-muted-border-color);
}

/* DROP */
.zone-title {
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1.2;
}

.monsters {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.35rem;
}

.monster {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  background: var(--pico-card-sectioning-background-color);
  border-radius: 6px;
  padding: 0.2rem 0.4rem;
}

.monster-icon {
  width: 28px;
  height: 28px;
}

.monster-name {
  font-size: 0.7rem;
  white-space: nowrap;
}

/* OTHER ZONES */
.other-zones {
  margin-top: 0.5rem;
  padding-top: 0.45rem;
  border-top: 1px dashed var(--pico-muted-border-color);
}

.other-zones-label {
  display: block;
  font-size: 0.7rem;
  color: var(--pico-muted-color);
  margin-bottom: 0.25rem;
}

.other-zones-list {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.other-zone {
  font-size: 0.72rem;
  color: var(--pico-color);
  opacity: 0.9;
}
</style>
