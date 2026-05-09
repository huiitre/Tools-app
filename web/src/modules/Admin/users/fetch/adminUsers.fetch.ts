import { clientV3 } from '@/services/axiosInstance'
import type { AdminUser, AdminRole } from '../types/adminUsers.types'

export async function fetchAdminUsers(): Promise<AdminUser[]> {
  const { data } = await clientV3.get<AdminUser[]>('/users')
  return data
}

export async function fetchAdminRoles(): Promise<AdminRole[]> {
  const { data } = await clientV3.get<AdminRole[]>('/roles')
  return data
}

export async function updateUserRole(userId: string, roleId: number): Promise<void> {
  await clientV3.put(`/users/${userId}/role`, { roleId })
}
