<script setup lang="ts">
import { computed } from 'vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import { useCatalogueGrid } from '@/modules/Dofus/catalogue/composables/useCatalogueGrid'

const props = withDefaults(defineProps<{ depth?: number }>(), { depth: 0 })

const store = useCatalogueStore()
const { gridTemplateColumns, getIndentPx } = useCatalogueGrid()

const visibleColumns = computed(() => store.visibleCatalogueColumns)

const isRightAligned = (key: string) =>
  key.includes('price') || key.startsWith('craft_')

const handleSort = (col: { key: string; sortable: boolean }) => {
  if (!col.sortable) return

  if (store.sort !== col.key) {
    store.setSort(col.key, 'ASC')
  } else if (store.dir === 'ASC') {
    store.setSort(col.key, 'DESC')
  } else {
    store.setSort(null, 'ASC')
  }
}

const getSortIcon = (key: string): string | null => {
  if (store.sort !== key) return null
  return store.dir === 'ASC' ? 'mdi-arrow-up' : 'mdi-arrow-down'
}

const allSelected = computed(() => store.allSelectableItemsSelected)
</script>

<template>
  <div
    class="catalogue-header"
    :style="{
      marginLeft: `${getIndentPx(props.depth)}px`,
      gridTemplateColumns,
    }"
  >
    <!-- Chevron placeholder -->
    <span />
    <!-- Image placeholder -->
    <span />
    <!-- Colonnes dynamiques -->
    <span
      v-for="col in visibleColumns"
      :key="col.key"
      :class="{
        right: isRightAligned(col.key),
        sortable: col.sortable,
        active: store.sort === col.key,
      }"
      :title="col.description"
      @click="handleSort(col)"
    >
      {{ col.label }}
      <i
        v-if="getSortIcon(col.key)"
        class="mdi sort-icon"
        :class="getSortIcon(col.key)"
      />
    </span>
    <!-- Checkbox "tout sélectionner" -->
    <div class="select-all">
      <input
        type="checkbox"
        :checked="allSelected"
        @change="store.toggleSelectAll()"
        title="Tout sélectionner / désélectionner"
      />
    </div>
  </div>
</template>

<style scoped lang="scss">
.catalogue-header {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0 0.6rem;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  overflow: hidden;
}

.right {
  text-align: right;
}

.sortable {
  cursor: pointer;
  user-select: none;

  &:hover {
    color: var(--pico-primary);
  }
}

.active {
  color: var(--pico-primary);
}

.sort-icon {
  margin-left: 0.25rem;
  font-size: 0.7rem;
}

.select-all {
  text-align: center;

  input[type="checkbox"] {
    margin: 0;
    cursor: pointer;
  }
}
</style>