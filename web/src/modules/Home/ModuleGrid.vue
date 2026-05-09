<script setup lang="ts">
import { useRouter } from 'vue-router'
import ModuleCard from './ModuleCard.vue'
import type { ModuleType } from '@/modules/Auth/auth.store'

defineProps<{
  modules: ModuleType[]
}>()

const router = useRouter()

const hasRouteFor = (module: ModuleType) => {
  return router.hasRoute(module.code)
}
</script>

<template>
  <div class="module-grid">
    <ModuleCard
      v-for="module in modules"
      :key="module.id"
      :module="module"
      :enabled="module.active && hasRouteFor(module)"
    />
  </div>
</template>

<style scoped lang="scss">
.module-grid {
  width: 100%;
  max-width: 1100px;
  padding: 0 1rem;

  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
}

/* Tablet */
@media (min-width: 640px) {
  .module-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

/* Desktop */
@media (min-width: 1024px) {
  .module-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>
