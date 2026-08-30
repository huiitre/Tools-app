import { clientCore } from '@/services/axiosInstance'
import type { TemtemDetail, TemtemSummary, TemtemType, TemtemTypeEffectiveness } from '@/modules/Temtem/shared/types/temtem.types'

/** Le catalogue entier en un appel : 165 Temtem, aucune pagination côté serveur. */
export async function fetchTemtem(): Promise<TemtemSummary[]> {
  const { data } = await clientCore.get<TemtemSummary[]>('/temtem/creatures')
  return data
}

export async function fetchTemtemTypes(): Promise<TemtemType[]> {
  const { data } = await clientCore.get<TemtemType[]>('/temtem/types')
  return data
}

/** La matrice entière (144 lignes) est gardée côté client pour les indications du simulateur. */
export async function fetchTemtemTypeEffectiveness(): Promise<TemtemTypeEffectiveness[]> {
  const { data } = await clientCore.get<TemtemTypeEffectiveness[]>('/temtem/types/effectiveness')
  return data
}

/** La fiche : le résumé, les techniques apprises (avec source et niveau) et les traits. */
export async function fetchTemtemDetail(slug: string): Promise<TemtemDetail> {
  const { data } = await clientCore.get<TemtemDetail>(`/temtem/creatures/${slug}`)
  return data
}
