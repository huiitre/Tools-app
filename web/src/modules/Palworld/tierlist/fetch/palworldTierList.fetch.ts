import { clientV3 } from '@/services/axiosInstance'
import type { PalworldTierListsByCategory } from '../types/palworldTierList.types'

export async function fetchPalworldTierLists(): Promise<PalworldTierListsByCategory> {
  const { data } = await clientV3.get<PalworldTierListsByCategory>('/palworld/tierlist')
  return data
}
