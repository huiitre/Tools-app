<script setup lang="ts">
import { onMounted, watch } from 'vue'
import CatalogueToolbar from '@/modules/Dofus/catalogue/components/CatalogueToolbar.vue'
import CatalogueHeader from '@/modules/Dofus/catalogue/components/CatalogueHeader.vue'
import CatalogueRow from '@/modules/Dofus/catalogue/components/CatalogueRow.vue'
import { useFetchColumns } from '@/modules/Dofus/catalogue/fetch/catalogue.fetch'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import toast from '@/services/toast'

const catalogueStore = useCatalogueStore()

const maxDepth = 6

/* =========================
   INIT
========================= */

onMounted(async () => {
  try {
    const { data: columns } = await useFetchColumns()
    catalogueStore.hydrateColumns(columns)
  } catch (e: any) {
    toast.error(e?.message ?? 'Erreur chargement colonnes')
  }
})

watch(
  () => [
    catalogueStore.q,
    catalogueStore.page,
    catalogueStore.pageSize,
    catalogueStore.sort,
    catalogueStore.dir,
  ],
  () => {
    if (catalogueStore.q === null ||catalogueStore.q.length === 0) return;
    catalogueStore.search()
  },
)
</script>

<template>
  <main id="dofus-catalogue">
    <CatalogueToolbar />

    <CatalogueHeader :depth="0" />

    <template
      v-for="item in catalogueStore.items"
      :key="item.id"
    >
      <CatalogueRow
        :item="item"
        :depth="0"
        :maxDepth="maxDepth"
      />
    </template>
  </main>
</template>

<style scoped lang="scss">
#dofus-catalogue {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  font-size: 0.85rem;
}
</style>