<script setup lang="ts">
import { useAuthStore } from '@/modules/Auth/auth.store';
import { computed } from 'vue';
import ModuleNotFound from './ModuleNotFound.vue';
import ModuleGrid from './ModuleGrid.vue';

const auth = useAuthStore()
const modules = computed(() => auth.user?.modules ?? [])

</script>

<template>
  <div id="home">
    <ModuleNotFound v-if="modules.length === 0" :modules="modules" />
    <ModuleGrid v-else :modules="modules" />
  </div>
</template>

<style lang="scss" scoped>
#home {
  width: 100%;
  margin: 3rem 0;
  display: flex;
  justify-content: center;
}
</style>
