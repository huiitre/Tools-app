import { computed } from 'vue'
import { useCatalogueStore } from '@/modules/Dofus/catalogue/catalogue.store'
import { storeToRefs } from 'pinia'

const CHEVRON_WIDTH = 24
const IMAGE_WIDTH = 36
const ACTIONS_WIDTH = 32
const INDENT_PER_LEVEL_PX = 24

export function useCatalogueGrid() {
  const store = useCatalogueStore()
  const { visibleCatalogueColumns } = storeToRefs(store)

  const gridTemplateColumns = computed(() => {
    const cols = visibleCatalogueColumns.value

    const dynamicParts = cols.map((col, index) => {
      const isLast = index === cols.length - 1

      if (col.grow === 0) {
        return `${col.size}px`
      }

      // Dernière colonne flexible : absorbe l'espace restant
      if (isLast) {
        return `minmax(${col.minSize}px, 1fr)`
      }

      return `minmax(${col.minSize}px, ${col.maxSize}px)`
    })

    return [
      `${CHEVRON_WIDTH}px`,
      `${IMAGE_WIDTH}px`,
      ...dynamicParts,
      `${ACTIONS_WIDTH}px`,
    ].join(' ')
  })

  const getIndentPx = (depth: number) => depth * INDENT_PER_LEVEL_PX

  return {
    gridTemplateColumns,
    getIndentPx,
    CHEVRON_WIDTH,
    IMAGE_WIDTH,
    ACTIONS_WIDTH,
  }
}