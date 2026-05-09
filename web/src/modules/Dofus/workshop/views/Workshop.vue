<script setup lang="ts">
import WorkshopSidebar from '@/modules/Dofus/workshop/components/WorkshopSidebar.vue'

</script>

<template>
  <main id="dofus-atelier">
    <WorkshopSidebar />

    <section class="content">
      <router-view :key="$route.fullPath" />
    </section>
  </main>
</template>

<style lang="scss" scoped>
#dofus-atelier {
  display: flex;
  flex: 1;
  background: var(--pico-background-color);
  color: var(--pico-color);
}

.content {
  flex: 1;
  padding: 2rem;
  // max-width: 1400px;
  margin: 0 auto;
  width: 100%;
}
</style>
