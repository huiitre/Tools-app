<script setup lang="ts">
defineProps<{
  show: boolean
}>()
</script>

<template>
  <div v-if="show" class="readonly-banner">
    <span class="mdi mdi-eye-outline"></span>
    <span class="text">
      Atelier en lecture seule
    </span>

    <button disabled>
      Importer dans mes ateliers
    </button>
  </div>
</template>

<style scoped lang="scss">
.readonly-banner {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: var(--pico-border-radius);
  background: rgba(255, 193, 7, 0.15);
  border: 1px solid rgba(255, 193, 7, 0.6);
  color: var(--pico-color);

  .text {
    flex: 1;
    font-size: 0.85rem;
    font-weight: 500;
  }

  button {
    font-size: 0.75rem;
  }
}
</style>
