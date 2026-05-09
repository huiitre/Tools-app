import { defineStore } from 'pinia'
import {
  CatalogueState,
  CatalogueColumn,
  CataloguePageSize,
  CatalogueSortDir,
} from '@/modules/Dofus/catalogue/types/catalogue.types'
import { useFetchSearch, useFetchRecipeByItemId } from '@/modules/Dofus/catalogue/fetch/catalogue.fetch'
import { useUIStore } from '@/stores/ui.store'
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices'
import { Item } from '@/modules/Dofus/item/types/item.types'

const STORAGE_KEY_COLUMNS = 'dofus.catalogue.columns'
const STORAGE_KEY_PAGE_SIZE = 'dofus.catalogue.page_size'

const DEFAULT_PAGE = 1
const DEFAULT_PAGE_SIZE = CataloguePageSize.M
const DEFAULT_SORT: string | null = null
const DEFAULT_DIR: CatalogueSortDir = 'ASC'

const DEFAULT_GROW_BY_KEY: Record<string, number> = {
  id: 0,
  asset_id: 0,
  type: 0,
  name: 2,
  level: 1,
  description: 3,

  quantity: 1,

  user_price: 1,
  community_average_price: 1,
  last_updated_price: 1,

  craft_user_price: 1,
  craft_community_price: 1,
  craft_last_price: 1,
  craft_calculated_price: 2,
}

export const useCatalogueStore = defineStore('dofus.catalogue', {
  state: (): CatalogueState => {
    const storedPageSize = localStorage.getItem(STORAGE_KEY_PAGE_SIZE)

    return {
      items: [],
      ingredients: new Map<number, Item[]>(),
      total: 0,

      q: null,

      page: DEFAULT_PAGE,
      pageSize: storedPageSize
        ? (parseInt(storedPageSize, 10) as CataloguePageSize)
        : DEFAULT_PAGE_SIZE,
      previousPage: null,
      nextPage: null,
      lastPage: null,

      sort: DEFAULT_SORT,
      dir: DEFAULT_DIR,

      columns: [],
      visibleColumns: new Set<string>(),

      selectedItemIds: new Set<number>(),

      error: null,
    }
  },

  getters: {
    visibleCatalogueColumns(state): CatalogueColumn[] {
      return state.columns.filter((c) => state.visibleColumns.has(c.key))
    },

    hasPreviousPage: (s) => s.previousPage !== null,
    hasNextPage: (s) => s.nextPage !== null,

    isItemSelected: (state) => (itemId: number) => state.selectedItemIds.has(itemId),
    
    selectedItemsWithRecipe(state): number[] {
      return state.items
        .filter(item => item.hasRecipe && state.selectedItemIds.has(item.id))
        .map(item => item.id)
    },

    allSelectableItemsSelected(state): boolean {
      const selectableItems = state.items.filter(item => item.hasRecipe)
      if (selectableItems.length === 0) return false
      return selectableItems.every(item => state.selectedItemIds.has(item.id))
    }
  },

  actions: {
    async search() {
      const uiStore = useUIStore()

      this.error = null
      this.clearSelection()

      try {
        uiStore.setLoading(true)
        const { data } = await useFetchSearch({
          q: this.q,
          page: this.page,
          pageSize: this.pageSize,
          sort: this.sort,
          dir: this.dir,
        })

        this.hydrateColumns(data.columns)
        this.setItems(
          data.items,
          data.total,
          data.page,
          data.pageSize,
          data.previousPage,
          data.nextPage,
          data.lastPage,
        )

        // Reset ingredients on new search
        this.ingredients = new Map<number, Item[]>()

        // Charger les prix de tous les items en une seule fois
        const { load } = useItemPrices()
        const itemIds = data.items.map((item: Item) => item.id)
        if (itemIds.length > 0) {
          await load(itemIds)
        }
      } catch (e: any) {
        this.error = e?.message ?? 'Erreur catalogue'
      } finally {
        uiStore.setLoading(false)
      }
    },

    async fetchIngredients(itemId: number) {
      if (this.ingredients.has(itemId)) return

      try {
        const { data } = await useFetchRecipeByItemId(itemId)
        this.ingredients.set(itemId, data)
        this.ingredients = new Map(this.ingredients)

        const { load } = useItemPrices()
        const ingredientIds = data.map((item: Item) => item.id)
        if (ingredientIds.length > 0) {
          await load(ingredientIds)
        }
      } catch (e: any) {
        console.error(`Erreur chargement ingrédients pour item ${itemId}:`, e)
      }
    },

    getIngredients(itemId: number): Item[] {
      return this.ingredients.get(itemId) ?? []
    },

    hydrateColumns(apiColumns: CatalogueColumn[]) {
      const storedRaw = localStorage.getItem(STORAGE_KEY_COLUMNS)
      const stored: CatalogueColumn[] = storedRaw ? JSON.parse(storedRaw) : []

      const storedByKey = new Map(stored.map((c) => [c.key, c]))

      const merged: CatalogueColumn[] = apiColumns.map((apiCol) => {
        const prev = storedByKey.get(apiCol.key)

        return {
          ...apiCol,
          visible: prev?.visible ?? apiCol.visible,
          grow: prev?.grow ?? DEFAULT_GROW_BY_KEY[apiCol.key] ?? 1,
        }
      })

      this.columns = merged
      this.visibleColumns = new Set(
        merged.filter((c) => c.visible).map((c) => c.key),
      )

      localStorage.setItem(STORAGE_KEY_COLUMNS, JSON.stringify(merged))
    },

    toggleColumn(key: string) {
      const col = this.columns.find((c) => c.key === key)
      if (!col || !col.userToggle) return

      col.visible = !col.visible

      col.visible
        ? this.visibleColumns.add(key)
        : this.visibleColumns.delete(key)

      localStorage.setItem(STORAGE_KEY_COLUMNS, JSON.stringify(this.columns))
    },

    setQuery(q: string | null) {
      this.q = q
      this.page = DEFAULT_PAGE
      this.clearSelection()
    },

    setPage(page: number) {
      this.page = page
      this.clearSelection()
    },

    setPageSize(size: CataloguePageSize) {
      this.pageSize = size
      this.page = DEFAULT_PAGE
      localStorage.setItem(STORAGE_KEY_PAGE_SIZE, String(size))
      this.clearSelection()
    },

    setSort(sort: string | null, dir: CatalogueSortDir) {
      this.sort = sort
      this.dir = dir
      this.page = DEFAULT_PAGE
      this.clearSelection()
    },

    setItems(
      items: Item[],
      total: number,
      page: number,
      pageSize: number,
      previousPage: number | null,
      nextPage: number | null,
      lastPage: number | null,
    ) {
      this.items = items
      this.total = total
      this.page = page
      this.pageSize = pageSize
      this.previousPage = previousPage
      this.nextPage = nextPage
      this.lastPage = lastPage
    },

    toggleItemSelection(itemId: number) {
      if (this.selectedItemIds.has(itemId)) {
        this.selectedItemIds.delete(itemId)
      } else {
        this.selectedItemIds.add(itemId)
      }
      this.selectedItemIds = new Set(this.selectedItemIds)
    },

    toggleSelectAll() {
      const selectableItems = this.items.filter(item => item.hasRecipe)
      
      if (this.allSelectableItemsSelected) {
        // Désélectionner tous
        selectableItems.forEach(item => this.selectedItemIds.delete(item.id))
      } else {
        // Sélectionner tous
        selectableItems.forEach(item => this.selectedItemIds.add(item.id))
      }
      
      this.selectedItemIds = new Set(this.selectedItemIds)
    },

    clearSelection() {
      this.selectedItemIds.clear()
      this.selectedItemIds = new Set()
    },

    clear() {
      this.columns = []
      this.visibleColumns.clear()
      this.ingredients = new Map<number, Item[]>()
      this.clearSelection()
      localStorage.removeItem(STORAGE_KEY_COLUMNS)
      localStorage.removeItem(STORAGE_KEY_PAGE_SIZE)
    },
  },
})