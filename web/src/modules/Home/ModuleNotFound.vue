<script setup lang="ts">
import { ModuleType } from '@/modules/Auth/auth.store';

defineProps<{
  modules: Array<ModuleType>
}>()

</script>

<template>
  <div v-if="modules.length === 0" class="empty-state">
    <span class="symbol">?</span>

    <p class="title">
      Aucun module n’est encore accessible
    </p>

    <p class="subtitle">
      Les modules disponibles apparaîtront ici
      une fois les accès activés.
    </p>
  </div>
</template>

<style lang="scss" scoped>
/* ===== Empty state ===== */

.empty-state {
  max-width: 420px;
  padding: 0 1.5rem;

  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 1rem;
}

.symbol {
  font-size: 5rem;
  font-weight: 700;
  line-height: 1;
  opacity: 0.15;
}

.title {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--pico-primary);
}

.subtitle {
  font-size: 0.9rem;
  opacity: 0.7;
}

/* ===== Desktop ===== */
@media (min-width: 768px) {
  .symbol {
    font-size: 6rem;
  }

  .title {
    font-size: 1.2rem;
  }
}
</style>