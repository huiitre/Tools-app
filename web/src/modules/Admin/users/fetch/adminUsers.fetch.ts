//* Administration servie par l'API Core. Les chemins sont identiques à ceux de l'API Java.
import { clientCore } from '@/services/axiosInstance'
import type { AdminUser, AdminRole } from '../types/adminUsers.types'

export async function fetchAdminUsers(): Promise<AdminUser[]> {
  const { data } = await clientCore.get<AdminUser[]>('/users')
  return data
}

export async function fetchAdminRoles(): Promise<AdminRole[]> {
  const { data } = await clientCore.get<AdminRole[]>('/roles')
  return data
}

export async function updateUserRole(userId: string, roleId: number): Promise<void> {
  await clientCore.put(`/users/${userId}/role`, { roleId })
}
