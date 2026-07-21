<script setup lang="ts">
import { computed } from 'vue'
import palworldMapImg from '@/assets/img/Palworld/palworld_map.png'
import { toMapPixel } from '../utils/palworldMap'

const props = defineProps<{
  player: { locationX: number; locationY: number; name: string } | null
  bases: Array<{ locationX: number; locationY: number; name: string }>
}>()

const playerPosition = computed(() => (props.player ? toMapPixel(props.player.locationX, props.player.locationY) : null))

const basePositions = computed(() =>
  props.bases
    .map(base => ({ base, position: toMapPixel(base.locationX, base.locationY) }))
    .filter((entry): entry is { base: typeof entry.base; position: NonNullable<typeof entry.position> } => entry.position !== null),
)
</script>

<template>
  <div class="mini-map">
    <div class="mini-map-frame">
      <img :src="palworldMapImg" alt="Carte du monde Palworld" class="mini-map-image">

      <i
        v-for="{ base, position } in basePositions"
        :key="base.name + position.xPercent"
        class="mdi mdi-home-variant map-marker map-marker--base"
        :style="{ left: position.xPercent + '%', top: position.yPercent + '%' }"
        :title="base.name"
      />

      <span
        v-if="playerPosition"
        class="map-marker map-marker--player"
        :style="{ left: playerPosition.xPercent + '%', top: playerPosition.yPercent + '%' }"
        :title="player?.name"
      />
    </div>

    <p v-if="player && !playerPosition" class="map-out-of-bounds">
      Position hors de la carte connue (autre zone, ex: The World Tree).
    </p>
  </div>
</template>

<style lang="scss" scoped>
.mini-map {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.mini-map-frame {
  position: relative;
  width: 100%;
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
}

.mini-map-image {
  display: block;
  width: 100%;
  height: auto;
}

.map-marker {
  position: absolute;
  transform: translate(-50%, -50%);
  border-radius: 50%;
  border: 2px solid white;
  box-shadow: 0 0 4px rgba(0, 0, 0, 0.6);
}

.map-marker--player {
  width: 12px;
  height: 12px;
  background: #3b82f6;
  z-index: 2;
}

.map-marker--base {
  border: none;
  border-radius: 0;
  box-shadow: none;
  font-size: 16px;
  line-height: 1;
  color: #f97316;
  text-shadow: 0 0 3px rgba(0, 0, 0, 0.9), 0 0 3px rgba(0, 0, 0, 0.9);
  z-index: 1;
}

.map-out-of-bounds {
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}
</style>
