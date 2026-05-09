import { clientV3Dofus } from '@/services/axiosInstance'
import { GameServerDto, GameVersionDto } from '../types/game.types'

export const useFetchGameServers = async () => {
  return await clientV3Dofus.get<GameServerDto[]>('/dofus/game-servers')
}

export const useFetchGameVersions = async () => {
  return await clientV3Dofus.get<GameVersionDto[]>('/dofus/game-versions')
}
