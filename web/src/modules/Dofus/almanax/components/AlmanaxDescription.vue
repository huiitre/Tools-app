<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  description: string
  tooltipEnabled?: boolean
}>()

const showTooltip = ref(false)
</script>

<template>
  <div
    class="almanax-desc-wrapper"
    :class="{ hover: tooltipEnabled }"
    @mouseenter="showTooltip = true"
    @mouseleave="showTooltip = false"
  >
    <div
      class="almanax-desc"
      :class="{ hover: tooltipEnabled, truncate: tooltipEnabled }"
      v-html="description"
    />

    <div
      v-if="tooltipEnabled && showTooltip"
      class="almanax-tooltip"
      role="tooltip"
      v-html="description"
    />
  </div>
</template>

<style lang="scss" scoped>
.almanax-desc-wrapper {
  position: relative;
  max-width: 100%;
}

.almanax-desc {
  color: var(--pico-muted-color);
  line-height: 1.2;
  border-radius: 4px;
  padding: 0.1rem 0.2rem;
  transition: background-color 0.15s ease;
}

.almanax-desc.truncate {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.almanax-desc-wrapper.hover:hover .almanax-desc.hover {
  background-color: color-mix(
    in srgb,
    var(--pico-primary) 12%,
    transparent
  );
}

.almanax-tooltip {
  position: absolute;
  top: 0;
  left: calc(100% + 0.5rem);
  z-index: 1000;

  width: 420px;
  max-width: 300px;
  padding: 0.5rem 0.65rem;

  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);

  font-size: 0.7rem;
  line-height: 1.3;
  color: var(--pico-color);

  white-space: normal;
  pointer-events: none;
  opacity: 1 !important;
  isolation: isolate;
}

/* Flip tooltip */
.calendar-grid > :nth-child(7n),
.calendar-grid > :nth-child(7n-1) {
  .almanax-tooltip {
    left: auto;
    right: calc(100% + 0.5rem);
  }
}

.calendar-grid > :nth-child(7n-6),
.calendar-grid > :nth-child(7n-5) {
  .almanax-tooltip {
    right: auto;
    left: calc(100% + 0.5rem);
  }
}
</style>
