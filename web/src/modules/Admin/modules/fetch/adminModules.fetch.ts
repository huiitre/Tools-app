//* Administration servie par l'API Core. Les chemins sont identiques à ceux de l'API Java.
//*
//* `updateModule` envoie le module **complet** : côté Core c'est un vrai PUT, un champ absent
//* serait écrasé et l'appel refusé sans `code` ni `name`.
import { clientCore } from '@/services/axiosInstance'
import type { AdminModule, ModuleUser, CreateModulePayload, UpdateModulePayload } from '../types/adminModules.types'
import type { AdminRole, AdminUser } from '../../users/types/adminUsers.types'

export async function fetchModules(): Promise<AdminModule[]> {
  const { data } = await clientCore.get<AdminModule[]>('/modules')
  return data
}

export async function createModule(payload: CreateModulePayload): Promise<AdminModule> {
  const { data } = await clientCore.post<AdminModule>('/modules', payload)
  return data
}

export async function updateModule(moduleId: number, payload: UpdateModulePayload): Promise<void> {
  await clientCore.put(`/modules/${moduleId}`, payload)
}

export async function fetchModuleUsers(moduleId: number): Promise<ModuleUser[]> {
  const { data } = await clientCore.get<ModuleUser[]>(`/modules/${moduleId}/users`)
  return data
}

export async function fetchRoles(): Promise<AdminRole[]> {
  const { data } = await clientCore.get<AdminRole[]>('/roles')
  return data
}

export async function addUserToModule(moduleId: number, userId: string): Promise<void> {
  await clientCore.post(`/modules/${moduleId}/users/${userId}`)
}

export async function updateUserModuleRole(moduleId: number, userId: string, roleId: number): Promise<void> {
  await clientCore.put(`/modules/${moduleId}/users/${userId}/role`, { roleId })
}

export async function removeUserFromModule(moduleId: number, userId: string): Promise<void> {
  await clientCore.delete(`/modules/${moduleId}/users/${userId}`)
}

export async function fetchAllUsers(): Promise<AdminUser[]> {
  const { data } = await clientCore.get<AdminUser[]>('/users')
  return data
}
