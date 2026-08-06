<script setup lang="ts">
import { computed, ref } from 'vue'
import palworldMapImg from '@/assets/img/Palworld/palworld_map.png'
import palworldMapWorldTreeImg from '@/assets/img/Palworld/palworld_map_worldtree.png'
import { resolvePalworldMap, type PalworldMapId } from '../utils/palworldMap'
import type { PalworldBase, PalworldGamePlayer } from '../types/palworldServer.types'
import type { PalworldGuildSummary } from '../types/palworldServerData.types'
import PalworldMapFrame, { type MapFrameMarker } from './PalworldMapFrame.vue'
import PalworldMapSidebar from './PalworldMapSidebar.vue'
import PalworldMapLayerSection, { type MapLayerItem } from './PalworldMapLayerSection.vue'

const props = defineProps<{
  players: PalworldGamePlayer[]
  bases: PalworldBase[]
  guilds?: PalworldGuildSummary[]
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
const activeMap = ref<PalworldMapId>('palpagos')

// Calques piloté par la colonne latérale : un élément décoché reste résolu/compté mais n'est plus rendu sur la carte.
const hiddenPlayerKeys = ref<Set<string>>(new Set())
const hiddenBaseKeys = ref<Set<string>>(new Set())

function toggleHidden(set: Set<string>, key: string): void {
  if (set.has(key)) set.delete(key)
  else set.add(key)
}

function guildColor(guildId: string): string {
  return props.guildColors[guildId] ?? DEFAULT_MARKER_COLOR
}

const mapPlayers = computed(() =>
  props.players
    .map(player => ({ player, resolution: resolvePalworldMap(player.locationX, player.locationY) }))
    .filter(
      (entry): entry is { player: PalworldGamePlayer; resolution: NonNullable<typeof entry.resolution> } =>
        entry.resolution !== null && entry.resolution.mapId === activeMap.value,
    ),
)

const mapBases = computed(() =>
  props.bases
    .map((base, index) => ({ base, index, resolution: resolvePalworldMap(base.locationX, base.locationY) }))
    .filter(
      (entry): entry is { base: PalworldBase; index: number; resolution: NonNullable<typeof entry.resolution> } =>
        entry.resolution !== null && entry.resolution.mapId === activeMap.value,
    )
    .map(entry => {
      const onlinePlayerNames = props.players
        .filter(player => player.guildId === entry.base.guildId)
        .map(player => player.name)
      const persistedPlayerNames = props.guilds
        ?.find(guild => guild.guildId === entry.base.guildId)
        ?.players.map(player => player.name) ?? []
      const guildPlayerNames = [...new Set([...onlinePlayerNames, ...persistedPlayerNames])]
      return { ...entry, key: `${entry.base.guildId}-${entry.index}`, guildPlayerNames }
    }),
)

const playerMarkers = computed<MapFrameMarker[]>(() =>
  mapPlayers.value
    .filter(({ player }) => !hiddenPlayerKeys.value.has(player.userId))
    .map(({ player, resolution }) => ({
      key: player.userId,
      xPercent: resolution.position.xPercent,
      yPercent: resolution.position.yPercent,
      color: guildColor(player.guildId),
      tooltip: `${player.name}\nNiveau ${player.level}\nPosition : ${player.mapX}, ${player.mapY}`,
    })),
)

const baseMarkers = computed<MapFrameMarker[]>(() =>
  mapBases.value
    .filter(({ key }) => !hiddenBaseKeys.value.has(key))
    .map(({ base, key, resolution, guildPlayerNames }) => {
      const tooltipLines = [base.guildName, base.name, `Position : ${base.mapX}, ${base.mapY}`]
      if (guildPlayerNames.length) {
        tooltipLines.push(`Joueurs : ${guildPlayerNames.join(', ')}`)
      }
      return {
        key,
        xPercent: resolution.position.xPercent,
        yPercent: resolution.position.yPercent,
        color: guildColor(base.guildId),
        tooltip: tooltipLines.join('\n'),
      }
    }),
)

const playerLayerItems = computed<MapLayerItem[]>(() =>
  [...mapPlayers.value]
    .sort((a, b) => a.player.name.localeCompare(b.player.name))
    .map(({ player }) => ({
      key: player.userId,
      label: player.name,
      sublabel: `Nv. ${player.level}`,
      color: guildColor(player.guildId),
    })),
)

const baseLayerItems = computed<MapLayerItem[]>(() =>
  [...mapBases.value]
    .sort((a, b) => b.guildPlayerNames.length - a.guildPlayerNames.length)
    .map(({ base, key, guildPlayerNames }) => ({
      key,
      label: base.guildName,
      sublabel: `${guildPlayerNames.length} joueur${guildPlayerNames.length > 1 ? 's' : ''}`,
      children: guildPlayerNames,
    })),
)

const activeMapLabel = computed(() => MAP_LABELS[activeMap.value])
const playerCountByMap = computed<Record<PalworldMapId, number>>(() => {
  const counts: Record<PalworldMapId, number> = { palpagos: 0, worldTree: 0 }
  for (const player of props.players) {
    const mapId = resolvePalworldMap(player.locationX, player.locationY)?.mapId
    if (mapId) counts[mapId]++
  }
  return counts
})

</script>

<template>
  <div class="overview-map">
    <ul class="overview-map-tabs" role="tablist" aria-label="Cartes du serveur">
      <li
        v-for="(label, mapId) in MAP_LABELS"
        :key="mapId"
        class="overview-map-tab"
        :class="{ active: activeMap === mapId }"
        :aria-selected="activeMap === mapId"
        role="tab"
        @click="activeMap = mapId"
      >
        {{ label }}
        <small>{{ playerCountByMap[mapId] }} joueur{{ playerCountByMap[mapId] > 1 ? 's' : '' }}</small>
      </li>
    </ul>

    <div class="overview-map-body">
      <PalworldMapSidebar>
        <PalworldMapLayerSection
          title="Joueurs"
          icon="mdi-account-group-outline"
          :items="playerLayerItems"
          :hidden-keys="hiddenPlayerKeys"
          empty-label="Aucun joueur connecté sur cette carte."
          @toggle="toggleHidden(hiddenPlayerKeys, $event)"
        />
        <PalworldMapLayerSection
          title="Bases"
          icon="mdi-home-group"
          :items="baseLayerItems"
          :hidden-keys="hiddenBaseKeys"
          empty-label="Aucune base sur cette carte."
          @toggle="toggleHidden(hiddenBaseKeys, $event)"
        />
      </PalworldMapSidebar>

      <div class="overview-map-canvas">
        <PalworldMapFrame
          :image-src="MAP_IMAGES[activeMap]"
          :image-alt="activeMapLabel"
          :players="playerMarkers"
          :bases="baseMarkers"
          interactive
        />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.overview-map {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  min-width: 0;
}

.overview-map-tabs {
  display: flex;
  gap: 2rem;
  margin: 0;
  padding: 0 0.25rem;
  list-style: none;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.overview-map-tab {
  position: relative;
  cursor: pointer;
  list-style: none;
  font-size: 0.85rem;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--pico-muted-color);
  padding: 0.5rem 0;
  transition: color 0.2s ease;
}

.overview-map-tab::after {
  content: '';
  position: absolute;
  left: 0;
  bottom: -1px;
  width: 100%;
  height: 1px;
  background-color: var(--pico-primary);
  transform: scaleX(0);
  transform-origin: center;
  transition: transform 0.25s ease;
}

.overview-map-tab:hover { color: var(--pico-color); }
.overview-map-tab.active { color: var(--pico-primary); }
.overview-map-tab.active::after { transform: scaleX(1); }

.overview-map-tab small {
  margin-left: 0.5rem;
  color: inherit;
  font-size: 0.72rem;
  font-weight: 500;
  text-transform: none;
  letter-spacing: normal;
  opacity: 0.75;
}

.overview-map-body {
  display: flex;
  align-items: stretch;
  gap: 0;
  min-width: 0;
  /* Colle la colonne au bord de la carte "Cartes" en compensant son padding (1.25rem, .maps-grid). */
  margin-left: -1.25rem;
  width: calc(100% + 1.25rem);
}

.overview-map-canvas {
  flex: 1;
  min-width: 0;
}

@media (max-width: 720px) {
  .overview-map-body {
    flex-direction: column;
  }
}
</style>
