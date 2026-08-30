import { clientCore } from '@/services/axiosInstance'
import type { TemtemTeam } from '@/modules/Temtem/teams/types/teams.types'

export async function fetchTeams(): Promise<TemtemTeam[]> {
  const { data } = await clientCore.get<TemtemTeam[]>('/temtem/teams')
  return data
}

/**
 * `temtemId` est facultatif : c'est ce qui permet à la popup du Temtemdex de créer l'équipe et
 * d'y placer le Temtem d'un seul geste, sans laisser d'équipe vide derrière un ajout raté.
 */
export async function createTeam(name: string, temtemId?: number): Promise<TemtemTeam> {
  const { data } = await clientCore.post<TemtemTeam>('/temtem/teams', { name, temtemId })
  return data
}

export async function addTeamMember(teamId: number, temtemId: number): Promise<TemtemTeam> {
  const { data } = await clientCore.post<TemtemTeam>(`/temtem/teams/${teamId}/members`, { temtemId })
  return data
}

export async function renameTeam(teamId: number, name: string): Promise<TemtemTeam> {
  const { data } = await clientCore.patch<TemtemTeam>(`/temtem/teams/${teamId}`, { name })
  return data
}

export async function deleteTeam(teamId: number): Promise<void> {
  await clientCore.delete(`/temtem/teams/${teamId}`)
}

export async function removeTeamMember(teamId: number, memberId: number): Promise<TemtemTeam> {
  const { data } = await clientCore.delete<TemtemTeam>(`/temtem/teams/${teamId}/members/${memberId}`)
  return data
}

/**
 * Remplacement total : la liste envoyée devient la liste retenue, elle ne s'y ajoute pas. Une
 * liste vide efface les techniques du membre, ce qui est un choix valable en cours de
 * composition.
 */
export async function setMemberTechniques(
  teamId: number,
  memberId: number,
  techniqueIds: number[],
): Promise<TemtemTeam> {
  const { data } = await clientCore.put<TemtemTeam>(
    `/temtem/teams/${teamId}/members/${memberId}/techniques`,
    { techniqueIds },
  )
  return data
}
