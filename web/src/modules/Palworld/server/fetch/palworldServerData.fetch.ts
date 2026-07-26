import { clientV3 } from '@/services/axiosInstance'
import type { PalworldGuildSummary } from '../types/palworldServerData.types'

export async function fetchGuilds(): Promise<PalworldGuildSummary[]> {
  const { data } = await clientV3.get<PalworldGuildSummary[]>('/palworld/server-data/guilds')
  return data
}
