import { clientCore } from '@/services/axiosInstance'
import type { GameServer, GameServerDetails, GameServerLive, GameServerMods } from '../types/gameServers.types'

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

export async function fetchGameServerMods(slug: string): Promise<GameServerMods> {
  const { data } = await clientCore.get<GameServerMods>(`/gameservers/${slug}/mods`)
  return data
}

export async function executeGameServerAction(
  slug: string,
  actionCode: string,
  parameters: Record<string, string>,
): Promise<void> {
  await clientCore.post(`/gameservers/${slug}/actions/${actionCode}`, parameters)
}

export async function executeGameServerRawCommand(slug: string, command: string): Promise<string | null> {
  const { data } = await clientCore.post<{ answer: string | null }>(`/gameservers/${slug}/raw-command`, { command })
  return data.answer
}
