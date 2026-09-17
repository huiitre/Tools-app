<script setup lang="ts">
import { computed } from 'vue'
import { useAdminAppLogsStore } from '../store/adminAppLogs.store'
import type { AppLogColumn } from '../types/adminAppLogs.types'

const store = useAdminAppLogsStore()
const visibleColumns = computed(() => store.visibleColumns)
const getSortIcon = (key: AppLogColumn['key']) => store.sortBy !== key ? null : store.sortDirection === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'
</script>

<template>
  <div class="logs-header" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <span v-for="column in visibleColumns" :key="column.key" :class="{ sortable: column.sortable, active: store.sortBy === column.key }" :title="column.description" @click="column.sortable && store.toggleSort(column.key)">
      {{ column.label }} <i v-if="getSortIcon(column.key)" class="mdi sort-icon" :class="getSortIcon(column.key)" />
    </span>
  </div>
</template>

<style scoped lang="scss">
.logs-header { display: grid; align-items: center; column-gap: 0.6rem; padding: 0 0.6rem; font-size: 0.75rem; color: var(--pico-muted-color); }
.sortable { cursor: pointer; user-select: none; &:hover { color: var(--pico-primary); } }
.active { color: var(--pico-primary); }
.sort-icon { margin-left: 0.25rem; font-size: 0.7rem; }
</style>
