<script setup lang="ts">
import { computed, ref } from 'vue'
import type { GameServerLivePlayer, GameServerLiveStructure } from '../types/gameServers.types'
import type { GameServerMapAdapter } from './mapAdapter'
import GameServerMapFrame, { type MapFrameMarker } from './GameServerMapFrame.vue'
import GameServerMapSidebar from './GameServerMapSidebar.vue'
import GameServerMapLayerSection, { type MapLayerItem } from './GameServerMapLayerSection.vue'

const props = withDefaults(defineProps<{
  adapter: GameServerMapAdapter
  players: GameServerLivePlayer[]
  structures: GameServerLiveStructure[]
  // Membres par groupe, déconnectés compris, quand le jeu sait les fournir.
  groups?: Record<string, string[]>
}>(), { groups: () => ({}) })

const GROUP_COLOR_PALETTE = [
  '#3b82f6', '#f97316', '#22c55e', '#a855f7', '#ef4444',
  '#06b6d4', '#eab308', '#ec4899', '#84cc16', '#6366f1',
]
const DEFAULT_MARKER_COLOR = '#94a3b8'

const activeMap = ref(props.adapter.maps[0]?.id ?? '')

// Un élément décoché reste listé et compté, il n'est simplement plus dessiné.
const hiddenPlayerKeys = ref<Set<string>>(new Set())
const hiddenStructureKeys = ref<Set<string>>(new Set())

function toggleHidden(set: Set<string>, key: string): void {
  if (set.has(key)) set.delete(key)
  else set.add(key)
}

// La couleur d'un groupe est stable tant que la composition du serveur ne change pas : elle vient
// de l'ordre alphabétique des identifiants, pas de l'ordre d'arrivée.
const groupColors = computed<Record<string, string>>(() => {
  const ids = Array.from(new Set([
    ...props.players.map(player => player.groupId),
    ...props.structures.map(structure => structure.groupId),
  ].filter((id): id is string => !!id))).sort()

  const colors: Record<string, string> = {}
  ids.forEach((id, index) => {
    colors[id] = GROUP_COLOR_PALETTE[index % GROUP_COLOR_PALETTE.length]
  })
  return colors
})

function groupColor(groupId: string | null): string {
  return (groupId && groupColors.value[groupId]) || DEFAULT_MARKER_COLOR
}

function playerKey(player: GameServerLivePlayer): string {
  return player.id ?? player.name
}

const positionedPlayers = computed(() =>
  props.players
    .map(player => ({
      player,
      position: player.positionX !== null && player.positionY !== null
        ? props.adapter.resolve(player.positionX, player.positionY)
        : null,
    }))
    .filter(entry => entry.position !== null && entry.position.mapId === activeMap.value)
)

const positionedStructures = computed(() =>
  props.structures
    .map(structure => ({
      structure,
      position: props.adapter.resolve(structure.positionX, structure.positionY),
    }))
    .filter(entry => entry.position !== null && entry.position.mapId === activeMap.value)
)

// Les membres d'un groupe servent à distinguer deux groupes qui portent le même nom. Le jeu les
// fournit tous quand il le peut ; sinon on ne connaît que ceux qui sont connectés.
function groupMemberNames(groupId: string | null): string[] {
  if (!groupId) return []
  return props.groups[groupId]
    ?? props.players.filter(player => player.groupId === groupId).map(player => player.name)
}

const playerMarkers = computed<MapFrameMarker[]>(() =>
  positionedPlayers.value
    .filter(({ player }) => !hiddenPlayerKeys.value.has(playerKey(player)))
    .map(({ player, position }) => {
      const lines = [player.name]
      if (player.level !== null) lines.push(`Niveau ${player.level}`)
      if (player.mapX !== null && player.mapY !== null) lines.push(`Position : ${player.mapX}, ${player.mapY}`)
      return {
        key: playerKey(player),
        xPercent: position!.xPercent,
        yPercent: position!.yPercent,
        color: groupColor(player.groupId),
        tooltip: lines.join('\n'),
      }
    })
)

const structureMarkers = computed<MapFrameMarker[]>(() =>
  positionedStructures.value
    .filter(({ structure }) => !hiddenStructureKeys.value.has(structure.key))
    .map(({ structure, position }) => {
      const lines = [structure.groupName ?? structure.name]
      if (structure.groupName) lines.push(structure.name)
      if (structure.creatureCount !== null) lines.push(`Pals : ${structure.creatureCount}`)
      const members = groupMemberNames(structure.groupId)
      if (members.length) lines.push(`Joueurs : ${members.join(', ')}`)
      return {
        key: structure.key,
        xPercent: position!.xPercent,
        yPercent: position!.yPercent,
        color: groupColor(structure.groupId),
        tooltip: lines.join('\n'),
      }
    })
)

const playerLayerItems = computed<MapLayerItem[]>(() =>
  [...positionedPlayers.value]
    .sort((a, b) => a.player.name.localeCompare(b.player.name))
    .map(({ player }) => ({
      key: playerKey(player),
      label: player.name,
      sublabel: player.level !== null ? `Nv. ${player.level}` : undefined,
      color: groupColor(player.groupId),
    }))
)

// Tri par nom de groupe, puis par membres : plusieurs guildes portent le même libellé (« Unnamed
// Guild » chez Palworld), et seuls leurs joueurs permettent alors de les regrouper.
const structureLayerItems = computed<MapLayerItem[]>(() =>
  [...positionedStructures.value]
    .sort((a, b) =>
      (a.structure.groupName ?? '').localeCompare(b.structure.groupName ?? '')
      || groupMemberNames(a.structure.groupId).join(', ').localeCompare(
        groupMemberNames(b.structure.groupId).join(', ')))
    .map(({ structure }) => {
      const members = groupMemberNames(structure.groupId)
      const parts: string[] = []
      if (members.length) parts.push(`${members.length} joueur${members.length > 1 ? 's' : ''}`)
      if (structure.creatureCount !== null) {
        parts.push(`${structure.creatureCount} pal${structure.creatureCount > 1 ? 's' : ''}`)
      }

      return {
        key: structure.key,
        label: structure.groupName ?? structure.name,
        sublabel: parts.length ? parts.join(' · ') : undefined,
        children: members,
      }
    })
)

const playerCountByMap = computed<Record<string, number>>(() => {
  const counts: Record<string, number> = {}
  for (const map of props.adapter.maps) counts[map.id] = 0
  for (const player of props.players) {
    if (player.positionX === null || player.positionY === null) continue
    const mapId = props.adapter.resolve(player.positionX, player.positionY)?.mapId
    if (mapId && mapId in counts) counts[mapId]++
  }
  return counts
})

const activeMapDefinition = computed(() =>
  props.adapter.maps.find(map => map.id === activeMap.value) ?? props.adapter.maps[0]
)
</script>

<template>
  <div class="overview-map">
    <ul v-if="adapter.maps.length > 1" class="overview-map-tabs" role="tablist" aria-label="Cartes du serveur">
      <li
        v-for="map in adapter.maps"
        :key="map.id"
        class="overview-map-tab"
        :class="{ active: activeMap === map.id }"
        :aria-selected="activeMap === map.id"
        role="tab"
        @click="activeMap = map.id"
      >
        {{ map.label }}
        <small>{{ playerCountByMap[map.id] }} joueur{{ playerCountByMap[map.id] > 1 ? 's' : '' }}</small>
      </li>
    </ul>

    <div class="overview-map-body">
      <GameServerMapSidebar>
        <GameServerMapLayerSection
          title="Joueurs"
          icon="mdi-account-group-outline"
          :items="playerLayerItems"
          :hidden-keys="hiddenPlayerKeys"
          empty-label="Aucun joueur connecté sur cette carte."
          @toggle="toggleHidden(hiddenPlayerKeys, $event)"
        />
        <GameServerMapLayerSection
          v-if="structures.length"
          title="Bases"
          icon="mdi-home-group"
          :items="structureLayerItems"
          :hidden-keys="hiddenStructureKeys"
          empty-label="Aucune base sur cette carte."
          @toggle="toggleHidden(hiddenStructureKeys, $event)"
        />
      </GameServerMapSidebar>

      <div class="overview-map-canvas">
        <GameServerMapFrame
          v-if="activeMapDefinition"
          :image-src="activeMapDefinition.image"
          :image-alt="activeMapDefinition.label"
          :players="playerMarkers"
          :bases="structureMarkers"
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
