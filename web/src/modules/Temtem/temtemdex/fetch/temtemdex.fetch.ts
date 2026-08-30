import { clientCore } from '@/services/axiosInstance'
import type { TemtemDetail, TemtemSummary, TemtemType } from '@/modules/Temtem/shared/types/temtem.types'

/** Le catalogue entier en un appel : 165 Temtem, aucune pagination côté serveur. */
export async function fetchTemtem(): Promise<TemtemSummary[]> {
  const { data } = await clientCore.get<TemtemSummary[]>('/temtem/creatures')
  return data
}

export async function fetchTemtemTypes(): Promise<TemtemType[]> {
  const { data } = await clientCore.get<TemtemType[]>('/temtem/types')
  return data
}

/** La fiche : le résumé, les techniques apprises (avec source et niveau) et les traits. */
export async function fetchTemtemDetail(slug: string): Promise<TemtemDetail> {
  const { data } = await clientCore.get<TemtemDetail>(`/temtem/creatures/${slug}`)
  return data
}
