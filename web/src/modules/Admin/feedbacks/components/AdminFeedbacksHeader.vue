<script setup lang="ts">
import { useAdminFeedbacksStore, COLUMNS } from '../store/adminFeedbacks.store'

const store = useAdminFeedbacksStore()

const getSortIcon = (key: string): string | null => {
  if (store.sort !== key) return null
  return store.dir === 'ASC' ? 'mdi-arrow-up' : 'mdi-arrow-down'
}
</script>

<template>
  <div class="feedbacks-header" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <span
      v-for="col in COLUMNS"
      :key="col.key"
      :class="{ sortable: col.sortable, active: store.sort === col.key }"
      @click="col.sortable && store.toggleSort(col.key)"
    >
      {{ col.label }}
      <i v-if="getSortIcon(col.key)" class="mdi sort-icon" :class="getSortIcon(col.key)" />
    </span>
  </div>
</template>

<style scoped lang="scss">
.feedbacks-header {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0 0.6rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.sortable {
  cursor: pointer;
  user-select: none;
  &:hover { color: var(--pico-primary); }
}

.active { color: var(--pico-primary); }

.sort-icon {
  margin-left: 0.25rem;
  font-size: 0.7rem;
}
</style>
