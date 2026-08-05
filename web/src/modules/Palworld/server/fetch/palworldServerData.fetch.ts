import { clientV3 } from '@/services/axiosInstance'
import type {
  PalworldGuildSummary,
  PalworldPalInstanceSummary,
  PalworldPalInstanceSnapshot,
  PalworldServerInventory,
} from '../types/palworldServerData.types'

export async function fetchGuilds(): Promise<PalworldGuildSummary[]> {
  const { data } = await clientV3.get<PalworldGuildSummary[]>('/palworld/server-data/guilds')
  return data
}

export async function fetchServerInventory(): Promise<PalworldServerInventory> {
  const { data } = await clientV3.get<PalworldServerInventory>('/palworld/server-data/inventory')
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
