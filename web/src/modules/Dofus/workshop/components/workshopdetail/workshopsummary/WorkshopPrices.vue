<script setup lang="ts">
interface Props {
  buyItems: number
  buyItemsRemaining: number
  craft: number
  craftAdjusted: number
  mixed: number
  mixedAdjusted: number
  //* Prix ajustés si les ressources prévues étaient complètes ; null sans prévision.
  preview: { buyItemsRemaining: number; craftAdjusted: number; mixedAdjusted: number } | null
}

defineProps<Props>()
const emit = defineEmits<{ (e: 'clear-preview'): void }>()

const PREVIEW_TITLE = 'Prévision : si les ressources prévues étaient complètes'
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
        <span class="value">
          <span v-if="preview" class="preview" :title="PREVIEW_TITLE">({{ preview.buyItemsRemaining.toLocaleString() }})</span>
          {{ buyItemsRemaining.toLocaleString() }} ₭
        </span>
      </div>
    </div>

    <div class="prices-row">
      <div class="price-cell">
        <span class="label">Prix craft total</span>
        <span class="value">{{ craft.toLocaleString() }} ₭</span>
      </div>
      <div class="price-cell">
        <span class="label">Prix craft ajusté</span>
        <span class="value">
          <span v-if="preview" class="preview" :title="PREVIEW_TITLE">({{ preview.craftAdjusted.toLocaleString() }})</span>
          {{ craftAdjusted.toLocaleString() }} ₭
        </span>
      </div>
    </div>

    <div class="prices-row highlight">
      <div class="price-cell">
        <span class="label">Meilleur prix</span>
        <span class="value">{{ mixed.toLocaleString() }} ₭</span>
      </div>
      <div class="price-cell">
        <span class="label">Meilleur prix ajusté</span>
        <span class="value">
          <span v-if="preview" class="preview" :title="PREVIEW_TITLE">({{ preview.mixedAdjusted.toLocaleString() }})</span>
          {{ mixedAdjusted.toLocaleString() }} ₭
        </span>
      </div>
    </div>

    <div v-if="preview" class="preview-row">
      <button class="clear-preview" @click="emit('clear-preview')">Effacer les prévisions</button>
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

.preview {
  font-weight: 400;
  color: var(--pico-primary);
  margin-right: 0.25rem;
  cursor: help;
}

.preview-row {
  display: flex;
  justify-content: flex-end;
}

.clear-preview {
  padding: 0.05rem 0.25rem;
  font-size: 0.65rem;
  line-height: 1.2;
  background: var(--pico-form-element-background-color);
  border: 1px solid var(--pico-form-element-border-color);
  color: var(--pico-form-element-color);
  border-radius: var(--pico-border-radius);
  cursor: pointer;
  transition: all 0.15s;
  margin: 0;
  width: auto;
  height: auto;

  &:hover {
    background: var(--pico-primary-background);
    border-color: var(--pico-primary-border);
    color: var(--pico-primary-inverse);
  }
}
</style>