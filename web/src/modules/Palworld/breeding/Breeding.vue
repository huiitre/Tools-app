<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import BreedingNav from './components/BreedingNav.vue'

const route = useRoute()
const isPathFinder = computed(() => route.name === 'palworld-breeding-path')
</script>

<template>
  <div class="breeding-layout" :class="{ 'breeding-layout--path': isPathFinder }">
    <BreedingNav />
    <div class="breeding-route">
      <router-view />
    </div>
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

.breeding-layout--path {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  overflow: hidden;
}

.breeding-layout--path .breeding-route {
  flex: 1;
  min-height: 0;
}

@media (max-width: 640px) {
  .breeding-layout {
    padding: 1rem 1rem 0;
  }
}

@media (max-width: 1050px) {
  .breeding-layout--path {
    height: auto;
    overflow: visible;
  }
}
</style>
