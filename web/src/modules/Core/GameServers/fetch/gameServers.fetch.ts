import { clientCore } from '@/services/axiosInstance'
import type { GameServer, GameServerDetails, GameServerLive } from '../types/gameServers.types'

export async function fetchGameServers(): Promise<GameServer[]> {
  const { data } = await clientCore.get<GameServer[]>('/gameservers')
  return data
}

export async function fetchGameServerDetails(slug: string): Promise<GameServerDetails> {
  const { data } = await clientCore.get<GameServerDetails>(`/gameservers/${slug}/details`)
  return data
}

export async function fetchGameServerLive(slug: string): Promise<GameServerLive> {
  const { data } = await clientCore.get<GameServerLive>(`/gameservers/${slug}/live`)
  return data
}

export async function executeGameServerAction(
  slug: string,
  actionCode: string,
  parameters: Record<string, string>,
): Promise<void> {
  await clientCore.post(`/gameservers/${slug}/actions/${actionCode}`, parameters)
}
