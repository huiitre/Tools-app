import { clientV3 } from '@/services/axiosInstance'
import type {
  PalworldGuildSummary,
  PalworldPalInstanceSummary,
  PalworldPalInstanceSnapshot,
} from '../types/palworldServerData.types'

export async function fetchGuilds(): Promise<PalworldGuildSummary[]> {
  const { data } = await clientV3.get<PalworldGuildSummary[]>('/palworld/server-data/guilds')
  return data
}

export async function fetchBasePals(baseId: string): Promise<PalworldPalInstanceSummary[]> {
  const { data } = await clientV3.get<PalworldPalInstanceSummary[]>(`/palworld/server-data/bases/${baseId}/pals`)
  return data
}

export async function fetchPalInstanceHistory(instanceId: string): Promise<PalworldPalInstanceSnapshot[]> {
  const { data } = await clientV3.get<PalworldPalInstanceSnapshot[]>(`/palworld/server-data/pal-instances/${instanceId}/history`)
  return data
}
