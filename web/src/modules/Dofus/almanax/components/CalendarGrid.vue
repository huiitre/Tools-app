<script setup lang="ts">
import AlmanaxDay from '@/modules/Dofus/almanax/components/AlmanaxDay.vue'
import { Almanax } from '@/modules/Dofus/almanax/types/almanax.types'

defineProps<{
  days: {
    date: Date
    iso: string
    isToday: boolean
    isCurrentMonth: boolean
    almanax?: Almanax
  }[]
}>()
</script>

<template>
  <section class="calendar-grid">
    <AlmanaxDay
      v-for="day in days"
      :key="day.iso"
      :day="day"
    />
  </section>
</template>

<style lang="scss" scoped>
.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.75rem;
}
</style>
