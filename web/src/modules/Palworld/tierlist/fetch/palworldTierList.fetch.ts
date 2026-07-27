import { clientV3 } from '@/services/axiosInstance'
import type { PalworldTierListsBySource } from '../types/palworldTierList.types'

export async function fetchPalworldTierLists(): Promise<PalworldTierListsBySource> {
  const { data } = await clientV3.get<PalworldTierListsBySource>('/palworld/tierlist')
  return data
}
