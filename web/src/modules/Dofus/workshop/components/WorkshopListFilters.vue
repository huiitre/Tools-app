<script setup lang="ts">
import { computed } from 'vue'
import { useWorkshopStore, WorkshopSortKey } from '@/modules/Dofus/workshop/store/workshop.store'

defineProps<{
  visibleCount: number
  totalCount: number
}>()

const store = useWorkshopStore()

const SORT_OPTIONS: { id: WorkshopSortKey; label: string }[] = [
  { id: 'name', label: 'Nom' },
]

const sortKey = computed({
  get: () => store.sortKey,
  set: (value: WorkshopSortKey) => store.setSort(value, store.sortDir),
})

const toggleSortDir = () => {
  store.setSort(store.sortKey, store.sortDir === 'asc' ? 'desc' : 'asc')
}
</script>

<template>
  <div class="workshop-filters">
    <div class="toolbar">
      <div class="sort-controls">
        <select v-model="sortKey" class="toolbar-select sort-select">
          <option v-for="opt in SORT_OPTIONS" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
        </select>

        <button
          type="button"
          class="toolbar-btn"
          :title="store.sortDir === 'asc' ? 'Croissant' : 'Décroissant'"
          @click="toggleSortDir"
        >
          <i class="mdi" :class="store.sortDir === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'" />
        </button>
      </div>

      <span class="count">
        <span class="count-sep" />
        <strong>{{ visibleCount }}</strong> / {{ totalCount }} ateliers
      </span>
    </div>

    <div v-if="store.tags.length" class="tag-tabs">
      <button
        v-for="tag in store.tags"
        :key="tag.id"
        type="button"
        class="tag-tab"
        :class="{ active: store.selectedTagIds.has(tag.id) }"
        @click="store.toggleSelectedTag(tag.id)"
      >
        <i class="mdi mdi-tag" :style="{ color: tag.color }" />
        {{ tag.name }}
      </button>

      <button
        v-if="store.selectedTagIds.size"
        type="button"
        class="clear-btn"
        @click="store.clearSelectedTags()"
      >
        Effacer les filtres
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.workshop-filters {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.sort-controls {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.toolbar-select {
  margin: 0;
  height: 2rem;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
  width: auto;

  &.sort-select { min-width: 150px; }
}

.toolbar-btn {
  margin: 0;
  height: 2rem;
  width: 2rem;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--pico-form-element-background-color);
  border: 1px solid var(--pico-form-element-border-color);
  color: var(--pico-form-element-color);
  cursor: pointer;
  border-radius: var(--pico-border-radius);
  flex-shrink: 0;
  transition: all 0.2s;

  &:hover {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }

  i { font-size: 1rem; }
}

.count {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  white-space: nowrap;

  strong { color: var(--pico-primary); font-weight: 700; }
}

.count-sep {
  display: inline-block;
  width: 1px;
  height: 1rem;
  background: var(--pico-muted-border-color);
  margin-right: 0.15rem;
}

.tag-tabs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.tag-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.8rem;
  border-radius: 999px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  font-size: 0.78rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin: 0;
  width: auto;

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-primary);
  }

  &.active {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
    font-weight: 700;
  }
}

.clear-btn {
  padding: 0.4rem 0.8rem;
  font-size: 0.78rem;
  width: auto;
  margin: 0;
  border-radius: 6px;
  border: 1px solid var(--pico-card-border-color);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  &:hover {
    color: var(--pico-contrast);
    border-color: var(--pico-contrast);
  }
}
</style>
