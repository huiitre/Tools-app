import { Item } from '@/modules/Dofus/item/types/item.types'

/* =========================
   COLONNES
========================= */

export type CatalogueColumn = {
  key: string
  label: string
  description: string
  visible: boolean
  sortable: boolean
  userToggle: boolean

  /** poids de largeur (0 = fixe / petit) */
  grow: number

  size: number
  minSize: number
  maxSize: number
}

export type BuildGridOptions = {
  columns: CatalogueColumn[]
  containerWidth: number
  leadingFixed?: number[]
  trailingFixed?: number[]
}

/* =========================
   PAGINATION / TRI
========================= */

export type CatalogueSortDir = 'ASC' | 'DESC'

export enum CataloguePageSize {
  XS = 10,
  S = 20,
  M = 50,
  L = 100,
}

/* =========================
   STATE DU STORE
========================= */

export type CatalogueState = {
  items: Item[]
  ingredients: Map<number, Item[]>
  total: number

  q: string | null

  page: number
  pageSize: CataloguePageSize
  previousPage: number | null
  nextPage: number | null
  lastPage: number | null

  sort: string | null
  dir: CatalogueSortDir

  columns: CatalogueColumn[]
  visibleColumns: Set<string>
  
  selectedItemIds: Set<number>

  error: string | null
}

/* =========================
   SEARCH DTO
========================= */

export type CatalogueSearch = {
  q: string | null
  page: number
  pageSize: CataloguePageSize
  sort: string | null
  dir: CatalogueSortDir
}