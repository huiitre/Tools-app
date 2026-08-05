import { clientV3 } from '@/services/axiosInstance'
import type { BreedingResult, BreedingCombination, BreedingPathResult } from '../types/breeding.types'

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

export async function fetchBreedingPath(targetId: number, ownedIds: number[]): Promise<BreedingPathResult> {
  const { data } = await clientV3.get<BreedingPathResult>('/palworld/breeding/path', {
    params: { target: targetId, owned: ownedIds.join(',') },
  })
  return data
}
