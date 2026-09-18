import { clientCore } from '@/services/axiosInstance'
import type {
  GameServer,
  GameServerDetails,
  GameServerLiveSnapshot,
  GameServerMods,
  GameServerRawCommandHistoryEntry,
} from '../types/gameServers.types'

export async function fetchGameServers(): Promise<GameServer[]> {
  const { data } = await clientCore.get<GameServer[]>('/gameservers')
  return data
}

export async function fetchGameServerDetails(slug: string): Promise<GameServerDetails> {
  const { data } = await clientCore.get<GameServerDetails>(`/gameservers/${slug}/details`)
  return data
}

// Cold-start uniquement (état courant sans attendre le prochain tick) : la mise à jour continue
// passe par l'event WebSocket Core.GameServersLiveUpdated, jamais par un nouvel appel à ceci.
export async function fetchGameServersLiveState(): Promise<GameServerLiveSnapshot[]> {
  const { data } = await clientCore.get<GameServerLiveSnapshot[]>('/gameservers/live-state')
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
  delaySeconds?: number,
): Promise<void> {
  await clientCore.post(`/gameservers/${slug}/actions/${actionCode}`, { parameters, delaySeconds })
}

export async function executeGameServerRawCommand(slug: string, command: string): Promise<string | null> {
  const { data } = await clientCore.post<{ answer: string | null }>(`/gameservers/${slug}/raw-command`, { command })
  return data.answer
}

export async function fetchGameServerRawCommandHistory(slug: string): Promise<GameServerRawCommandHistoryEntry[]> {
  const { data } = await clientCore.get<GameServerRawCommandHistoryEntry[]>(`/gameservers/${slug}/raw-command/history`)
  return data
}
