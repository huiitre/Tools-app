<script setup lang="ts">
defineProps<{ id: number }>()
</script>

<template>
  <span class="skin-id-badge">{{ id }}</span>
</template>

<style scoped>
.skin-id-badge {
  position: absolute;
  top: 0.35rem;
  left: 0.4rem;
  font-size: 0.6rem;
  font-family: monospace;
  color: rgba(255, 255, 255, 0.4);
  padding: 0.1rem 0.35rem;
  line-height: 1.5;
  pointer-events: none;
  user-select: text;
  opacity: 0;
  transition: opacity 0.2s ease;
}
</style>
