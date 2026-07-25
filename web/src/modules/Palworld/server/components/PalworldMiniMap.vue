<script setup lang="ts">
import { computed } from 'vue'
import palworldMapImg from '@/assets/img/Palworld/palworld_map.png'
import palworldMapWorldTreeImg from '@/assets/img/Palworld/palworld_map_worldtree.png'
import { resolvePalworldMap, type PalworldMapId } from '../utils/palworldMap'

const props = defineProps<{
  player: { locationX: number; locationY: number; name: string } | null
  bases: Array<{ locationX: number; locationY: number; name: string }>
}>()

const MAP_LABELS: Record<PalworldMapId, string> = {
  palpagos: 'Palpagos Islands',
  worldTree: "L'Arbre Monde",
}

const MAP_IMAGES: Record<PalworldMapId, string> = {
  palpagos: palworldMapImg,
  worldTree: palworldMapWorldTreeImg,
}

const playerResolution = computed(() =>
  props.player ? resolvePalworldMap(props.player.locationX, props.player.locationY) : null,
)

const activeMapId = computed<PalworldMapId>(() => playerResolution.value?.mapId ?? 'palpagos')

const basePositions = computed(() =>
  props.bases
    .map(base => ({ base, resolution: resolvePalworldMap(base.locationX, base.locationY) }))
    .filter(
      (entry): entry is { base: typeof entry.base; resolution: NonNullable<typeof entry.resolution> } =>
        entry.resolution !== null && entry.resolution.mapId === activeMapId.value,
    ),
)
</script>

<template>
  <div class="mini-map">
    <p class="mini-map-label">
      {{ MAP_LABELS[activeMapId] }}
    </p>

    <div class="mini-map-frame">
      <img :src="MAP_IMAGES[activeMapId]" alt="Carte du monde Palworld" class="mini-map-image">

      <i
        v-for="{ base, resolution } in basePositions"
        :key="base.name + resolution.position.xPercent"
        class="mdi mdi-home-variant map-marker map-marker--base"
        :style="{ left: resolution.position.xPercent + '%', top: resolution.position.yPercent + '%' }"
        :title="base.name"
      />

      <span
        v-if="playerResolution"
        class="map-marker map-marker--player"
        :style="{ left: playerResolution.position.xPercent + '%', top: playerResolution.position.yPercent + '%' }"
        :title="player?.name"
      />
    </div>

    <p v-if="player && !playerResolution" class="map-out-of-bounds">
      Position hors des cartes connues.
    </p>
  </div>
</template>

<style lang="scss" scoped>
.mini-map {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.mini-map-label {
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
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
