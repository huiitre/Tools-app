<script setup lang="ts">
import { useClipboard } from '@/composables/useClipboard';

const props = defineProps<{
  level: number
  parentName?: string
}>()

const { copy } = useClipboard()

function getLevelColor(level: number): string {
  const colors = [
    '#22c55e',
    '#3b82f6',
    '#f59e0b',
    '#a855f7',
    '#ef4444'
  ]
  return colors[Math.min(level - 1, colors.length - 1)]
}
</script>

<template>
  <div
    class="craft-banner"
    :style="{ borderLeftColor: getLevelColor(props.level) }"
  >
    <div
      class="craft-level"
      :style="{ backgroundColor: getLevelColor(props.level) }"
    >
      {{ props.level }}
    </div>
    <i class="mdi mdi-package-variant"></i>
    <span>Pour craft : <span class="copyable" @click="copy(props.parentName ?? '')">{{ props.parentName }}</span></span>
  </div>
</template>

<style scoped>
.craft-banner {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.5rem;
  margin: -0.75rem -0.75rem 0 -0.75rem;
  background: var(--pico-card-sectioning-background-color);
  border-bottom: 1px solid var(--pico-muted-border-color);
  border-left: 3px solid;
  font-size: 0.75rem;
  font-weight: 600;
}

.craft-level {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 0.65rem;
  font-weight: 700;
}

.craft-banner i {
  font-size: 0.9rem;
  color: var(--pico-muted-color);
}
</style>
