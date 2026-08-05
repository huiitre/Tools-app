<script setup lang="ts">
import BreedingPalChip from './BreedingPalChip.vue'
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import type { BreedingPathNode } from '../types/breeding.types'

const { node, resolvePal, isRoot } = defineProps<{
  node: BreedingPathNode
  resolvePal: (id: number) => PalworldPalListItem | null
  isRoot?: boolean
}>()
</script>

<template>
  <div class="path-node">
    <div class="path-node-card" :class="{ owned: node.owned }">
      <i v-if="isRoot" class="mdi mdi-crown path-node-crown" />
      <BreedingPalChip :pal="resolvePal(node.species.id)" :size="48" />
      <span v-if="node.step" class="path-node-rule">
        {{ node.step.rule === 'exception' ? 'Exception' : 'Formule' }}
      </span>
    </div>

    <div v-if="node.step" class="path-node-children">
      <div class="path-node-branch">
        <span v-if="node.step.parentAGender" class="path-node-gender" :class="node.step.parentAGender">
          <i class="mdi" :class="node.step.parentAGender === 'Male' ? 'mdi-gender-male' : 'mdi-gender-female'" />
        </span>
        <BreedingPathTreeNode :node="node.step.parentA" :resolve-pal="resolvePal" />
      </div>

      <div class="path-node-branch">
        <span v-if="node.step.parentBGender" class="path-node-gender" :class="node.step.parentBGender">
          <i class="mdi" :class="node.step.parentBGender === 'Male' ? 'mdi-gender-male' : 'mdi-gender-female'" />
        </span>
        <BreedingPathTreeNode :node="node.step.parentB" :resolve-pal="resolvePal" />
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
