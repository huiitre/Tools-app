<script setup lang="ts">
import { useAuthStore } from '@/modules/Auth/auth.store';
import { computed } from 'vue';
import ModuleNotFound from './ModuleNotFound.vue';
import ModuleGrid from './ModuleGrid.vue';
import GameServersWidget from '@/modules/Core/GameServers/components/GameServersWidget.vue';

const auth = useAuthStore()
const modules = computed(() => auth.user?.modules ?? [])

</script>

<template>
  <div id="home">
    <GameServersWidget />

    <section class="modules-section">
      <h4 class="modules-title">Modules</h4>
      <ModuleNotFound v-if="modules.length === 0" :modules="modules" />
      <ModuleGrid v-else :modules="modules" />
    </section>
  </div>
</template>

<style lang="scss" scoped>
#home {
  width: 100%;
  margin: 3rem 0;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.modules-section {
  width: 100%;
  max-width: 1100px;
  padding: 0 1rem;
}

.modules-title {
  margin: 0 0 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--pico-muted-color);
}
</style>
