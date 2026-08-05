import { clientV3 } from '@/services/axiosInstance'
import type { BreedingResult, BreedingCombination } from '../types/breeding.types'
import type { BreedingRuleData } from '../services/BreedingPathEngine'

export async function fetchBreedingResult(
  parentA: number,
  parentB: number,
  genderA?: 'Male' | 'Female' | null,
  genderB?: 'Male' | 'Female' | null,
): Promise<BreedingResult> {
  const { data } = await clientV3.get<BreedingResult>('/palworld/breeding/result', {
    params: { parentA, parentB, genderA: genderA ?? undefined, genderB: genderB ?? undefined },
  })
  return data
}

export async function fetchBreedingParents(childId: number): Promise<BreedingCombination[]> {
  const { data } = await clientV3.get<BreedingCombination[]>('/palworld/breeding/parents', {
    params: { child: childId },
  })
  return data
}

export async function fetchBreedingAsParent(palId: number): Promise<BreedingCombination[]> {
  const { data } = await clientV3.get<BreedingCombination[]>('/palworld/breeding/as-parent', {
    params: { pal: palId },
  })
  return data
}

export async function fetchBreedingRules(): Promise<BreedingRuleData[]> {
  const { data } = await clientV3.get<BreedingRuleData[]>('/palworld/breeding/rules')
  return data
}
