<script setup lang="ts">
import { computed, ref } from 'vue'
import palworldMapImg from '@/assets/img/Palworld/palworld_map.png'
import palworldMapWorldTreeImg from '@/assets/img/Palworld/palworld_map_worldtree.png'
import { resolvePalworldMap, type PalworldMapId } from '../utils/palworldMap'
import type { PalworldBase, PalworldGamePlayer } from '../types/palworldServer.types'
import PalworldMapFrame, { type MapFrameMarker } from './PalworldMapFrame.vue'

const props = defineProps<{
  mapId: PalworldMapId
  players: PalworldGamePlayer[]
  bases: PalworldBase[]
  guildColors: Record<string, string>
}>()

const MAP_LABELS: Record<PalworldMapId, string> = {
  palpagos: 'Palpagos Islands',
  worldTree: "L'Arbre Monde",
}

const MAP_IMAGES: Record<PalworldMapId, string> = {
  palpagos: palworldMapImg,
  worldTree: palworldMapWorldTreeImg,
}

const DEFAULT_MARKER_COLOR = '#94a3b8'

function guildColor(guildId: string): string {
  return props.guildColors[guildId] ?? DEFAULT_MARKER_COLOR
}

const playerMarkers = computed<MapFrameMarker[]>(() =>
  props.players
    .map(player => ({ player, resolution: resolvePalworldMap(player.locationX, player.locationY) }))
    .filter(
      (entry): entry is { player: PalworldGamePlayer; resolution: NonNullable<typeof entry.resolution> } =>
        entry.resolution !== null && entry.resolution.mapId === props.mapId,
    )
    .map(({ player, resolution }) => ({
      key: player.userId,
      xPercent: resolution.position.xPercent,
      yPercent: resolution.position.yPercent,
      color: guildColor(player.guildId),
      tooltip: `${player.name}\nNiveau ${player.level}\nPosition : ${player.mapX}, ${player.mapY}`,
    })),
)

const baseMarkers = computed<MapFrameMarker[]>(() =>
  props.bases
    .map((base, index) => ({ base, index, resolution: resolvePalworldMap(base.locationX, base.locationY) }))
    .filter(
      (entry): entry is { base: PalworldBase; index: number; resolution: NonNullable<typeof entry.resolution> } =>
        entry.resolution !== null && entry.resolution.mapId === props.mapId,
    )
    .map(({ base, index, resolution }) => {
      const guildPlayerNames = props.players.filter(player => player.guildId === base.guildId).map(player => player.name)
      const tooltipLines = [base.guildName, base.name, `Position : ${base.mapX}, ${base.mapY}`]
      if (guildPlayerNames.length) {
        tooltipLines.push(`Joueurs : ${guildPlayerNames.join(', ')}`)
      }
      return {
        key: `${base.guildId}-${index}`,
        xPercent: resolution.position.xPercent,
        yPercent: resolution.position.yPercent,
        color: guildColor(base.guildId),
        tooltip: tooltipLines.join('\n'),
      }
    }),
)

const enlarged = ref(false)
</script>

<template>
  <div class="overview-map">
    <p class="overview-map-label">
      {{ MAP_LABELS[mapId] }}
      <span class="overview-map-count">{{ playerMarkers.length }} joueur{{ playerMarkers.length > 1 ? 's' : '' }}</span>
    </p>

    <div class="overview-map-thumb" @click="enlarged = true">
      <PalworldMapFrame
        :image-src="MAP_IMAGES[mapId]"
        :image-alt="MAP_LABELS[mapId]"
        :players="playerMarkers"
        :bases="baseMarkers"
      />
    </div>

    <Teleport to="body">
      <div v-if="enlarged" class="overview-map-overlay" @click.self="enlarged = false">
        <div class="overview-map-overlay-content">
          <div class="overview-map-overlay-header">
            <span>
              {{ MAP_LABELS[mapId] }}
              <span class="overview-map-count">{{ playerMarkers.length }} joueur{{ playerMarkers.length > 1 ? 's' : '' }}</span>
            </span>
            <i class="mdi mdi-close overview-map-overlay-close" @click="enlarged = false" />
          </div>
          <PalworldMapFrame
            :image-src="MAP_IMAGES[mapId]"
            :image-alt="MAP_LABELS[mapId]"
            :players="playerMarkers"
            :bases="baseMarkers"
          />
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style lang="scss" scoped>
.overview-map {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  min-width: 0;
}

.overview-map-label {
  margin: 0;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--pico-muted-color);
}

.overview-map-count {
  margin-left: 0.4rem;
  font-weight: 500;
  font-size: 0.72rem;
  color: var(--pico-primary);
  background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
  border-radius: 999px;
  padding: 0.1rem 0.55rem;
}

.overview-map-thumb {
  cursor: pointer;
  transition: opacity 0.15s ease;

  &:hover {
    opacity: 0.85;
  }
}

.overview-map-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  z-index: 1100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.overview-map-overlay-content {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  width: 100%;
  max-width: 900px;
  max-height: 90vh;
  overflow-y: auto;
  padding: 1rem;
}

.overview-map-overlay-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.overview-map-overlay-close {
  cursor: pointer;
  color: var(--pico-muted-color);
  font-size: 1.2rem;

  &:hover {
    color: var(--pico-color);
  }
}

@media (max-width: 640px) {
  .overview-map-overlay {
    padding: 0.5rem;
  }
}
</style>
