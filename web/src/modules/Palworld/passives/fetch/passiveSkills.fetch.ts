import { clientV3 } from '@/services/axiosInstance'
import type { PalworldPassiveSkill } from '../types/passiveSkills.types'

export async function fetchPassiveSkills(): Promise<PalworldPassiveSkill[]> {
  const { data } = await clientV3.get<PalworldPassiveSkill[]>('/palworld/passive-skills')
  return data
}
