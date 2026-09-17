<script setup lang="ts">
defineProps<{ title: string; content: string; visible: boolean; x: number; y: number }>()
</script>

<template>
  <div v-if="visible" class="app-log-context-floating" :style="{ left: `${x}px`, top: `${y}px` }">
    <div class="title">{{ title }}</div>
    <pre>{{ content }}</pre>
  </div>
</template>

<style scoped>
.app-log-context-floating { position: fixed; z-index: 1000; pointer-events: none; max-width: min(520px, calc(100vw - 2rem)); background: var(--pico-card-background-color); border: 1px solid color-mix(in srgb, var(--pico-primary) 30%, transparent); box-shadow: var(--pico-card-box-shadow); border-radius: 10px; padding: .75rem; font-size: .8rem; color: var(--pico-color); }
.title { margin-bottom: .4rem; color: var(--pico-primary); font-weight: 600; }
pre { max-height: 320px; margin: 0; overflow: hidden; white-space: pre-wrap; word-break: break-word; font: .75rem var(--pico-font-family-monospace); }
</style>
