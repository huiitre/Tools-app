<script setup lang="ts">
interface Props {
  items: number
  totalRes: number
  missing: number
  complete: number
}

defineProps<Props>()
</script>

<template>
  <div class="stats-grid">
    <div class="stat-box">
      <strong>{{ items }}</strong>
      <span>Items</span>
    </div>
    <div class="stat-box">
      <strong>{{ totalRes }}</strong>
      <span>Ressources</span>
    </div>
    <div class="stat-box warn">
      <strong>{{ missing }}</strong>
      <span>Manquantes</span>
    </div>
    <div class="stat-box ok">
      <strong>{{ complete }}</strong>
      <span>Complètes</span>
    </div>
  </div>
</template>

<style scoped>
.stats-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.5rem;
}

.stat-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.5rem;
  background: var(--pico-card-sectioning-background-color);
  border-radius: var(--pico-border-radius);
  border: 1px solid var(--pico-muted-border-color);
}

.stat-box strong {
  font-size: 1.25rem;
}

.stat-box span {
  font-size: 0.7rem;
  color: var(--pico-muted-color);
}

.stat-box.warn strong {
  color: var(--pico-del-color);
}

.stat-box.ok strong {
  color: var(--pico-ins-color);
}
</style>