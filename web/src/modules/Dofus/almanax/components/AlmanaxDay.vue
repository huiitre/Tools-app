<script setup lang="ts">
import { Almanax } from '@/modules/Dofus/almanax/types/almanax.types'
import AlmanaxDescription from './AlmanaxDescription.vue'
import AlmanaxItem from './AlmanaxItem.vue'

const props = defineProps<{
  day: {
    date: Date
    iso: string
    isToday: boolean
    isCurrentMonth: boolean
    almanax?: Almanax
  }
}>()

const tooltipEnabled = false

const onAddToCalendar = () => {
  console.log('add to calendar:', props.day.iso)
}
</script>

<template>
  <article
    class="calendar-day"
    :class="{ today: day.isToday, outside: !day.isCurrentMonth }"
  >
    <!-- Header -->
    <div class="day-header">
      <span class="day-number">
        {{ day.date.getDate() }}
      </span>

      <i
        v-if="day.almanax"
        class="mdi mdi-calendar-plus calendar-icon"
        title="Ajouter au calendrier"
        @click.stop="onAddToCalendar"
      />
    </div>

    <!-- Contenu Almanax -->
    <div v-if="day.almanax" class="day-content">
      <div class="almanax-name" v-html="day.almanax.name" />

      <div class="almanax-content">
        <AlmanaxDescription
          :description="day.almanax.description"
          :tooltip-enabled="tooltipEnabled"
        />

        <AlmanaxItem
          v-if="day.almanax.item"
          class="almanax-item"
          :item="day.almanax.item"
          :quantity="day.almanax.quantity"
        />
      </div>
    </div>
  </article>
</template>

<style lang="scss" scoped>
.calendar-day {
  position: relative;
  background: var(--pico-card-background-color);
  border-radius: 8px;
  padding: 0.5rem 0.6rem;
  min-height: 140px;
  display: flex;
  flex-direction: column;
  margin: 0;
}

.calendar-day:not(.today) {
  border: 1px solid var(--pico-card-border-color);
}

.calendar-day.outside {
  filter: grayscale(1) brightness(0.9);
}

.calendar-day.today {
  border: 2px solid color-mix(
    in srgb,
    var(--pico-primary) 60%,
    transparent
  );
}

/* Header */
.day-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.35rem;
}

.day-number {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.calendar-icon {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  cursor: pointer;
  border-radius: 4px;
  padding: 0.15rem;

  &:hover {
    color: var(--pico-primary);
    background: var(--pico-card-background-color);
  }
}

/* Content */
.day-content {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.75rem;
  flex: 1;
}

.almanax-name {
  font-weight: 600;
  font-size: 0.8rem;
  color: var(--pico-primary);
}

/* Almanax content layout */
.almanax-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  gap: 0.5rem;
}

.almanax-item {
  margin-top: auto;
}
</style>
