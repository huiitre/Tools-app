<script setup lang="ts">
interface Props {
  buyItems: number
  buyItemsRemaining: number
  craft: number
  craftAdjusted: number
  mixed: number
  mixedAdjusted: number
}

defineProps<Props>()
</script>

<template>
  <div class="prices">
    <div class="prices-row">
      <div class="price-cell">
        <span class="label">Prix d'achat total</span>
        <span class="value">{{ buyItems.toLocaleString() }} ₭</span>
      </div>
      <div class="price-cell">
        <span class="label">Prix d'achat ajusté</span>
        <span class="value">{{ buyItemsRemaining.toLocaleString() }} ₭</span>
      </div>
    </div>

    <div class="prices-row">
      <div class="price-cell">
        <span class="label">Prix craft total</span>
        <span class="value">{{ craft.toLocaleString() }} ₭</span>
      </div>
      <div class="price-cell">
        <span class="label">Prix craft ajusté</span>
        <span class="value">{{ craftAdjusted.toLocaleString() }} ₭</span>
      </div>
    </div>

    <div class="prices-row highlight">
      <div class="price-cell">
        <span class="label">Prix mixte total</span>
        <span class="value">{{ mixed.toLocaleString() }} ₭</span>
      </div>
      <div class="price-cell">
        <span class="label">Prix mixte ajusté</span>
        <span class="value">{{ mixedAdjusted.toLocaleString() }} ₭</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.prices {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  padding: 0.5rem;
  background: var(--pico-card-sectioning-background-color);
  border-radius: var(--pico-border-radius);
  border: 1px solid var(--pico-muted-border-color);
}

.prices-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  font-size: 0.75rem;
}

.price-cell {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
}

.label {
  flex-shrink: 0;
}

.value {
  font-weight: 600;
  white-space: nowrap;
}

.prices-row.highlight .value {
  color: var(--pico-ins-color);
}
</style>