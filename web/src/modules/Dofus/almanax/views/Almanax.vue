<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAlmanaxStore } from '@/modules/Dofus/almanax/almanax.store'
import AlmanaxNav from '@/modules/Dofus/almanax/components/AlmanaxNav.vue'
import CalendarGrid from '@/modules/Dofus/almanax/components/CalendarGrid.vue'
import CalendarWeekdays from '@/modules/Dofus/almanax/components/CalendarWeekdays.vue'
import FullPageLoader from '@/components/ui/FullPageLoader.vue'
import toast from '@/services/toast'

const almanaxStore = useAlmanaxStore()

const today = new Date()
const todayISO =
  today.getFullYear() + '-' +
  String(today.getMonth() + 1).padStart(2, '0') + '-' +
  String(today.getDate()).padStart(2, '0')

const displayedYear = ref(today.getFullYear())
const displayedMonth = ref(today.getMonth())

onMounted(async () => {
  try {
    await almanaxStore.fetch()
  } catch (error: any) {
    toast.error(error?.message || 'Erreur lors du chargement des données Almanax')
  }
})

const canGoPrev = computed(() => {
  const minDate = almanaxStore.minDate
  if (!minDate) return false
  return (
    displayedYear.value > minDate.getFullYear() ||
    (
      displayedYear.value === minDate.getFullYear() &&
      displayedMonth.value > minDate.getMonth()
    )
  )
})

const canGoNext = computed(() => {
  const maxDate = almanaxStore.maxDate
  if (!maxDate) return false
  return (
    displayedYear.value < maxDate.getFullYear() ||
    (
      displayedYear.value === maxDate.getFullYear() &&
      displayedMonth.value < maxDate.getMonth()
    )
  )
})

const isCurrentMonth = computed(() => {
  return (
    displayedYear.value === today.getFullYear() &&
    displayedMonth.value === today.getMonth()
  )
})

const goPrevMonth = () => {
  if (!canGoPrev.value) return
  displayedMonth.value === 0
    ? (displayedMonth.value = 11, displayedYear.value--)
    : displayedMonth.value--
}

const goNextMonth = () => {
  if (!canGoNext.value) return
  displayedMonth.value === 11
    ? (displayedMonth.value = 0, displayedYear.value++)
    : displayedMonth.value++
}

const goToday = () => {
  displayedYear.value = today.getFullYear()
  displayedMonth.value = today.getMonth()
}

const days = computed(() => {
  const result = []
  const firstOfMonth = new Date(displayedYear.value, displayedMonth.value, 1)
  const firstDayIndex = (firstOfMonth.getDay() + 6) % 7
  const startDate = new Date(displayedYear.value, displayedMonth.value, 1 - firstDayIndex)

  for (let i = 0; i < 35; i++) {
    const d = new Date(startDate)
    d.setDate(startDate.getDate() + i)
    const iso =
      d.getFullYear() + '-' +
      String(d.getMonth() + 1).padStart(2, '0') + '-' +
      String(d.getDate()).padStart(2, '0')

    result.push({
      date: d,
      iso,
      isToday: iso === todayISO,
      isCurrentMonth: d.getMonth() === displayedMonth.value,
      almanax: almanaxStore.almanaxByDate.get(iso),
    })
  }

  return result
})
</script>

<template>
  <div id="dofus-almanax">
    <AlmanaxNav
      :year="displayedYear"
      :month="displayedMonth"
      :can-go-prev="canGoPrev"
      :can-go-next="canGoNext"
      :is-current-month="isCurrentMonth"
      @prev="goPrevMonth"
      @next="goNextMonth"
      @today="goToday"
    />

    <CalendarWeekdays />
    <CalendarGrid :days="days" />

    <FullPageLoader :visible="almanaxStore.loading" />
  </div>
</template>

<style lang="scss" scoped>
#dofus-almanax {
  padding: 0.5rem;
}
</style>