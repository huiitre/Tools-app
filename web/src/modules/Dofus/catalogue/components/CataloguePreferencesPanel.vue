<script setup lang="ts">
import { computed } from 'vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import { CataloguePageSize } from '@/modules/Dofus/catalogue/types/catalogue.types'

const catalogueStore = useCatalogueStore()

const pageSizes = Object.values(CataloguePageSize).filter(
  v => typeof v === 'number',
) as number[]

const userToggleColumns = computed(() =>
  catalogueStore.columns.filter(c => c.userToggle),
)

const onToggleColumn = (key: string) => {
  catalogueStore.toggleColumn(key)
}
</script>

<template>
  <div class="catalogue-preferences-panel">
    <h3 class="panel-title">Catalogue</h3>

    <!-- PAGE SIZE -->
    <div class="pref-block">
      <label class="pref-label">Taille de page</label>

      <select
        class="pref-select"
        :value="catalogueStore.pageSize"
        @change="
          catalogueStore.setPageSize(
            Number(($event.target as HTMLSelectElement).value)
          )
        "
      >
        <option
          v-for="size in pageSizes"
          :key="size"
          :value="size"
        >
          {{ size }} éléments
        </option>
      </select>
    </div>

    <!-- COLONNES -->
    <div class="pref-block">
      <label class="pref-label">Colonnes</label>

      <div
        v-for="col in userToggleColumns"
        :key="col.key"
        class="pref-switch"
      >
        <span>{{ col.label }}</span>

        <label>
          <input
            type="checkbox"
            role="switch"
            :checked="col.visible"
            @change="onToggleColumn(col.key)"
          />
        </label>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.catalogue-preferences-panel {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;

  min-width: 220px;
  font-size: 0.75rem;
}

.panel-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
}

.pref-block {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.pref-label {
  font-weight: 500;
  color: var(--pico-primary);
}

.pref-select {
  font-size: 0.75rem;
  margin: 0;
}

.pref-switch {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}
</style>
