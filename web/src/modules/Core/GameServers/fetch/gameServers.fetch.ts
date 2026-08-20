import { clientCore } from '@/services/axiosInstance'
import type { GameServer } from '../types/gameServers.types'

export async function fetchGameServers(): Promise<GameServer[]> {
  const { data } = await clientCore.get<GameServer[]>('/gameservers')
  return data
}
