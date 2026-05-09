import { clientV3 } from '@/services/axiosInstance'
import type { AdminModule, ModuleUser, CreateModulePayload, UpdateModulePayload } from '../types/adminModules.types'
import type { AdminRole, AdminUser } from '../../users/types/adminUsers.types'

export async function fetchModules(): Promise<AdminModule[]> {
  const { data } = await clientV3.get<AdminModule[]>('/modules')
  return data
}

export async function createModule(payload: CreateModulePayload): Promise<AdminModule> {
  const { data } = await clientV3.post<AdminModule>('/modules', payload)
  return data
}

export async function updateModule(moduleId: number, payload: UpdateModulePayload): Promise<AdminModule> {
  const { data } = await clientV3.put<AdminModule>(`/modules/${moduleId}`, payload)
  return data
}

export async function fetchModuleUsers(moduleId: number): Promise<ModuleUser[]> {
  const { data } = await clientV3.get<ModuleUser[]>(`/modules/${moduleId}/users`)
  return data
}

export async function fetchRoles(): Promise<AdminRole[]> {
  const { data } = await clientV3.get<AdminRole[]>('/roles')
  return data
}

export async function addUserToModule(moduleId: number, userId: string): Promise<void> {
  await clientV3.post(`/modules/${moduleId}/users/${userId}`)
}

export async function updateUserModuleRole(moduleId: number, userId: string, roleId: number): Promise<void> {
  await clientV3.put(`/modules/${moduleId}/users/${userId}/role`, { roleId })
}

export async function removeUserFromModule(moduleId: number, userId: string): Promise<void> {
  await clientV3.delete(`/modules/${moduleId}/users/${userId}`)
}

export async function fetchAllUsers(): Promise<AdminUser[]> {
  const { data } = await clientV3.get<AdminUser[]>('/users')
  return data
}
