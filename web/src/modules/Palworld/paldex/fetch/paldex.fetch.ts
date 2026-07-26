import { clientV3 } from '@/services/axiosInstance'
import type { PalworldPalListItem } from '../types/paldex.types'

export async function fetchPals(): Promise<PalworldPalListItem[]> {
  const { data } = await clientV3.get<PalworldPalListItem[]>('/palworld/pals')
  return data
}
