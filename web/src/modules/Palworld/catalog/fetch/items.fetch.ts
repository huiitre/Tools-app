import { clientV3 } from '@/services/axiosInstance'
import type { ItemCatalogEntry } from '../types/items.types'

export async function fetchItems(): Promise<ItemCatalogEntry[]> {
  const { data } = await clientV3.get<ItemCatalogEntry[]>('/palworld/items')
  return data
}
