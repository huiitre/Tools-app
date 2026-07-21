import { clientV3 } from '@/services/axiosInstance'
import type {
  PalworldGameData,
  PalworldServerInfo,
  PalworldServerMetrics,
  PalworldServerPlayer,
  PalworldServerSettings,
} from '../types/palworldServer.types'

export async function fetchServerInfo(): Promise<PalworldServerInfo> {
  const { data } = await clientV3.get<PalworldServerInfo>('/palworld/server/info')
  return data
}

export async function fetchServerSettings(): Promise<PalworldServerSettings> {
  const { data } = await clientV3.get<PalworldServerSettings>('/palworld/server/settings')
  return data
}

export async function fetchServerPlayers(): Promise<PalworldServerPlayer[]> {
  const { data } = await clientV3.get<PalworldServerPlayer[]>('/palworld/server/players')
  return data
}

export async function fetchServerMetrics(): Promise<PalworldServerMetrics> {
  const { data } = await clientV3.get<PalworldServerMetrics>('/palworld/server/metrics')
  return data
}

export async function fetchServerGameData(): Promise<PalworldGameData> {
  const { data } = await clientV3.get<PalworldGameData>('/palworld/server/game-data')
  return data
}

export async function announceServerMessage(message: string): Promise<void> {
  await clientV3.post('/palworld/server/announce', { message })
}

export async function kickServerPlayer(userId: string, message: string): Promise<void> {
  await clientV3.post('/palworld/server/kick', { userId, message })
}

export async function saveServerWorld(): Promise<void> {
  await clientV3.post('/palworld/server/save')
}

export async function banServerPlayer(userId: string, message: string): Promise<void> {
  await clientV3.post('/palworld/server/ban', { userId, message })
}

export async function unbanServerPlayer(userId: string): Promise<void> {
  await clientV3.post('/palworld/server/unban', { userId })
}

export async function shutdownServer(waittime: number, message: string): Promise<void> {
  await clientV3.post('/palworld/server/shutdown', { waittime, message })
}

export async function stopServer(): Promise<void> {
  await clientV3.post('/palworld/server/stop')
}
