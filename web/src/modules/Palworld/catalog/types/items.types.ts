export interface ItemCatalogEntry {
  id: number
  slug: string
  name: string
  iconUrl: string | null
  category: string | null
  price: number | null
  soldByMerchant: boolean
}
