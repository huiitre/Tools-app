<script setup lang="ts">
import BreedingPalChip from './BreedingPalChip.vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { PalworldPassiveSkill } from '../../passives/types/passiveSkills.types'
import type { BreedingPathNode } from '../types/breeding.types'
import { computed } from 'vue'
import { usePalworldServerDataStore } from '../../server/serverData.store'

const props = defineProps<{
  node: BreedingPathNode
  resolvePal: (id: number) => PalworldPalListItem | null
  passiveSkills: PalworldPassiveSkill[]
  showGenders: boolean
  isRoot?: boolean
}>()
const serverDataStore = usePalworldServerDataStore()

const passiveSkillById = computed(() => new Map(props.passiveSkills.map(passiveSkill => [passiveSkill.id, passiveSkill])))
const nodePassives = computed(() => props.node.passiveSkillIds
  .map(passiveSkillId => passiveSkillById.value.get(passiveSkillId))
  .filter((passiveSkill): passiveSkill is PalworldPassiveSkill => passiveSkill !== undefined))
const parentAGender = computed(() => props.showGenders ? props.node.step?.parentA.gender ?? null : props.node.step?.parentAGender ?? null)
const parentBGender = computed(() => props.showGenders ? props.node.step?.parentB.gender ?? null : props.node.step?.parentBGender ?? null)
const storageLocationLabel = computed(() => {
  if (props.node.storageLocation === null) return null
  return {
    base: 'Base',
    palbox: 'Palbox',
    party: 'Équipe',
    dimensional_storage: 'Boîte dimensionnelle',
  }[props.node.storageLocation]
})
</script>

<template>
  <div class="path-node">
    <div class="path-node-card" :class="{ owned: props.node.owned }">
      <span v-if="serverDataStore.selectedPalCounts.get(props.node.species.id)" class="server-pal-count">{{ serverDataStore.selectedPalCounts.get(props.node.species.id) }}</span>
      <i v-if="props.isRoot" class="mdi mdi-crown path-node-crown" />
      <BreedingPalChip
        :pal="props.resolvePal(props.node.species.id)"
        :gender="props.showGenders ? props.node.gender : null"
        :size="48"
        show-catalog-details
      />
      <span v-if="props.node.step" class="path-node-rule">
        {{ props.node.step.rule === 'exception' ? 'Exception' : 'Formule' }}
      </span>
      <span v-if="props.node.owned && storageLocationLabel" class="path-node-location">
        {{ storageLocationLabel }}
      </span>
      <div v-if="nodePassives.length" class="path-node-passives">
        <span
          v-for="passiveSkill in nodePassives"
          :key="passiveSkill.id"
          :title="passiveSkill.description ?? passiveSkill.name"
          :class="`rank-${passiveSkill.rank}`"
        >
          <img v-if="passiveSkill.rankIconUrl" :src="passiveSkill.rankIconUrl" alt="">
          <i v-else class="mdi mdi-chevron-double-up" aria-hidden="true" />
          {{ passiveSkill.name }}
        </span>
      </div>
    </div>

    <div v-if="props.node.step" class="path-node-children">
      <div class="path-node-branch">
        <span v-if="parentAGender" class="path-node-gender" :class="parentAGender">
          <i class="mdi" :class="parentAGender === 'Male' ? 'mdi-gender-male' : 'mdi-gender-female'" />
        </span>
        <BreedingPathTreeNode
          :node="props.node.step.parentA"
          :resolve-pal="props.resolvePal"
          :passive-skills="props.passiveSkills"
          :show-genders="props.showGenders"
        />
      </div>

      <div class="path-node-branch">
        <span v-if="parentBGender" class="path-node-gender" :class="parentBGender">
          <i class="mdi" :class="parentBGender === 'Male' ? 'mdi-gender-male' : 'mdi-gender-female'" />
        </span>
        <BreedingPathTreeNode
          :node="props.node.step.parentB"
          :resolve-pal="props.resolvePal"
          :passive-skills="props.passiveSkills"
          :show-genders="props.showGenders"
        />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
$line-color: var(--pico-muted-border-color);
$connector-height: 1rem;
$branch-gap: .9rem;
$half-branch-gap: .45rem;

.path-node {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.path-node-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.6rem;
  border-radius: 10px;
  background: color-mix(in srgb, #f59e0b 12%, var(--pico-card-background-color));
  border: 2px solid #f59e0b;
  z-index: 1;

  &.owned {
    background: color-mix(in srgb, #22c55e 12%, var(--pico-card-background-color));
    border-color: #22c55e;
  }
}

.server-pal-count {
  position: absolute;
  top: .35rem;
  right: .35rem;
  z-index: 2;
  display: grid;
  place-items: center;
  min-width: 1.15rem;
  height: 1.15rem;
  padding: 0 .2rem;
  border: 1px solid var(--pico-primary);
  border-radius: 999px;
  background: var(--pico-card-background-color);
  color: var(--pico-primary);
  font-size: .6rem;
  font-weight: 800;
}

.path-node-crown {
  color: #f59e0b;
  font-size: 1.1rem;
}

.path-node-rule {
  font-size: 0.65rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  white-space: nowrap;
}

.path-node-location {
  color: var(--pico-muted-color);
  font-size: .54rem;
  font-weight: 600;
  text-transform: uppercase;
}

.path-node-passives {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: .2rem;
  max-width: 170px;

  > span {
    display: inline-flex;
    align-items: center;
    gap: .12rem;
    max-width: 100%;
    padding: .12rem .26rem;
    border: 1px solid var(--passive-rank-color);
    border-radius: 999px;
    color: var(--pico-color);
    font-size: .52rem;
    font-weight: 700;
    line-height: 1;
    white-space: nowrap;
  }

  img, i { width: .7rem; height: .7rem; object-fit: contain; color: var(--passive-rank-color); }
}

.rank-5, .rank-4 { --passive-rank-color: #42d9ff; }
.rank-3, .rank-2 { --passive-rank-color: #f5df39; }
.rank-1 { --passive-rank-color: #dceaf0; }
.rank--1, .rank--2, .rank--3 { --passive-rank-color: #ff4d63; }

/* ── Connecteurs de l'arbre (grid 2 colonnes égales : le centre de chaque
   branche tombe exactement à 25%/75% du conteneur, donc la ligne horizontale
   entre les deux est toujours pile alignée, quelle que soit la largeur réelle
   des cartes) ── */
.path-node-children {
  position: relative;
  display: grid;
  grid-template-columns: max-content max-content;
  column-gap: $branch-gap;
  margin-top: $connector-height;

  &::after {
    content: '';
    position: absolute;
    top: -$connector-height;
    left: 50%;
    width: 0;
    height: $connector-height;
    border-left: 2px solid #{$line-color};
  }
}

.path-node-branch {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: $connector-height;

  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 50%;
    width: 0;
    height: $connector-height;
    border-left: 2px solid #{$line-color};
  }

  &:first-child::after,
  &:last-child::after {
    content: '';
    position: absolute;
    top: 0;
    border-top: 2px solid #{$line-color};
  }

  &:first-child::after {
    left: 50%;
    width: calc(50% + #{$half-branch-gap});
  }

  &:last-child::after {
    right: 50%;
    width: calc(50% + #{$half-branch-gap});
  }
}

.path-node-gender {
  position: absolute;
  top: calc(#{$connector-height} * 2 - 0.55rem);
  left: 50%;
  transform: translateX(-50%);
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.1rem;
  height: 1.1rem;
  border-radius: 50%;
  background: var(--pico-background-color);
  font-size: 0.7rem;

  &.Male { color: #4a90d9; }
  &.Female { color: #d9598f; }
}
</style>
