<script setup lang="ts">
import type { PalworldPalListItem } from '../../paldex/types/paldex.types'
import BreedingPalChip from './BreedingPalChip.vue'

defineProps<{
  parentA: PalworldPalListItem | null
  parentB: PalworldPalListItem | null
  child: PalworldPalListItem | null
  parentAGender: 'Male' | 'Female' | null
  parentBGender: 'Male' | 'Female' | null
  rule: 'exception' | 'formula'
}>()
</script>

<template>
  <div class="combination-row">
    <BreedingPalChip :pal="parentA" :gender="parentAGender" layout="vertical" />
    <span class="op">+</span>
    <BreedingPalChip :pal="parentB" :gender="parentBGender" layout="vertical" />
    <span class="op">=</span>
    <BreedingPalChip :pal="child" layout="vertical" />
    <span class="rule-badge" :class="rule">{{ rule === 'exception' ? 'Exception' : 'Formule' }}</span>
  </div>
</template>

<style lang="scss" scoped>
.combination-row {
  position: relative;
  display: grid;
  grid-template-columns: minmax(72px, 1fr) auto minmax(72px, 1fr) auto minmax(72px, 1fr);
  align-items: center;
  justify-items: center;
  column-gap: 0.5rem;
  row-gap: 0.6rem;
  padding: 0.75rem 0.65rem;
  border-radius: 8px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
}

.op {
  color: var(--pico-muted-color);
  font-weight: 700;
  font-size: 0.9rem;
}

.rule-badge {
  position: absolute;
  top: 0.4rem;
  right: 0.5rem;
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  color: var(--pico-muted-color);

  &.exception {
    color: var(--pico-primary);
    border-color: var(--pico-primary);
  }
}

@media (max-width: 480px) {
  .combination-row {
    grid-template-columns: minmax(56px, 1fr) auto minmax(56px, 1fr) auto minmax(56px, 1fr);
    column-gap: 0.35rem;
    padding-inline: 0.4rem;
  }
}
</style>
