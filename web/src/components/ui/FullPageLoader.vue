<script setup lang="ts">
defineProps<{
  visible: boolean
}>()
</script>

<template>
  <div
    v-if="visible"
    class="fullpage-loader"
    role="status"
    aria-live="polite"
    aria-busy="true"
  >
    <span aria-hidden="true"></span>
  </div>
</template>

<style scoped>
.fullpage-loader {
  position: fixed;
  inset: 0;
  z-index: 1500;

  display: flex;
  align-items: center;
  justify-content: center;

  background-color: #000000b4;

  font-size: 4rem;
  opacity: 1;
}

.fullpage-loader :deep(span) {
  color: var(--pico-primary);
}
</style>
