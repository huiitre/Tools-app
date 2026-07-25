<script setup lang="ts">
export interface MapFrameMarker {
  key: string
  xPercent: number
  yPercent: number
  color: string
  tooltip: string
}

defineProps<{
  imageSrc: string
  imageAlt: string
  players: MapFrameMarker[]
  bases: MapFrameMarker[]
}>()
</script>

<template>
  <div class="map-frame">
    <img :src="imageSrc" :alt="imageAlt" class="map-frame-image">

    <i
      v-for="base in bases"
      :key="base.key"
      class="mdi mdi-home-variant map-frame-marker map-frame-marker--base"
      :style="{ left: base.xPercent + '%', top: base.yPercent + '%', color: base.color }"
      :title="base.tooltip"
    />

    <span
      v-for="player in players"
      :key="player.key"
      class="map-frame-marker map-frame-marker--player"
      :style="{ left: player.xPercent + '%', top: player.yPercent + '%', background: player.color }"
      :title="player.tooltip"
    />
  </div>
</template>

<style lang="scss" scoped>
.map-frame {
  position: relative;
  width: 100%;
  border-radius: var(--pico-border-radius);
  overflow: hidden;
  border: 1px solid var(--pico-card-border-color);
}

.map-frame-image {
  display: block;
  width: 100%;
  height: auto;
}

.map-frame-marker {
  position: absolute;
  transform: translate(-50%, -50%);
}

.map-frame-marker--player {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid white;
  box-shadow: 0 0 4px rgba(0, 0, 0, 0.6);
  z-index: 2;
}

.map-frame-marker--base {
  font-size: 18px;
  line-height: 1;
  text-shadow: 0 0 3px rgba(0, 0, 0, 0.9), 0 0 3px rgba(0, 0, 0, 0.9);
  z-index: 1;
}
</style>
