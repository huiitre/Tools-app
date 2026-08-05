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
    <BreedingPalChip :pal="parentA" :gender="parentAGender" />
    <span class="op">+</span>
    <BreedingPalChip :pal="parentB" :gender="parentBGender" />
    <span class="op">=</span>
    <BreedingPalChip :pal="child" />
    <span class="rule-badge" :class="rule">{{ rule === 'exception' ? 'Exception' : 'Formule' }}</span>
  </div>
</template>

<style lang="scss" scoped>
.combination-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 0.85rem;
  border-radius: 8px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  flex-wrap: wrap;
}

.op {
  color: var(--pico-muted-color);
  font-weight: 700;
  font-size: 0.9rem;
}

.rule-badge {
  margin-left: auto;
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
</style>
