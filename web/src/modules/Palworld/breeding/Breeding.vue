<script setup lang="ts">
import BreedingNav from './components/BreedingNav.vue'
</script>

<template>
  <div class="breeding-layout">
    <BreedingNav />
    <router-view />
  </div>
</template>

<style lang="scss" scoped>
.breeding-layout {
  padding: 1.5rem 2rem 0;
  max-width: 1800px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
}

@media (max-width: 640px) {
  .breeding-layout {
    padding: 1rem 1rem 0;
  }
}
</style>
