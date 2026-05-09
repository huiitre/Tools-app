<script setup lang="ts">
import { computed } from 'vue'
import { useAdminUsersStore } from '../store/adminUsers.store'

const store = useAdminUsersStore()
const visibleColumns = computed(() => store.visibleColumns)

const handleSort = (col: { key: string; sortable: boolean }) => {
  if (!col.sortable) return
  store.toggleSort(col.key)
}

const getSortIcon = (key: string): string | null => {
  if (store.sort !== key) return null
  return store.dir === 'ASC' ? 'mdi-arrow-up' : 'mdi-arrow-down'
}
</script>

<template>
  <div class="users-header" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <!-- Avatar placeholder -->
    <span />
    <span
      v-for="col in visibleColumns"
      :key="col.key"
      :class="{ sortable: col.sortable, active: store.sort === col.key }"
      :title="col.description"
      @click="handleSort(col)"
    >
      {{ col.label }}
      <i v-if="getSortIcon(col.key)" class="mdi sort-icon" :class="getSortIcon(col.key)" />
    </span>
  </div>
</template>

<style scoped lang="scss">
.users-header {
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
