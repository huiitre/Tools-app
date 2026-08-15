//* Administration servie par l'API Core. Les chemins sont identiques à ceux de l'API Java.
import { clientCore } from '@/services/axiosInstance'

export interface UserPerModule {
  moduleId: string
  moduleName: string
  userCount: number
}

export interface AdminStats {
  totalUsers: number
  activeUsers: number
  newUsersThisWeek: number
  usersPerModule: UserPerModule[]
}

export async function fetchAdminStats(): Promise<AdminStats> {
  const { data } = await clientCore.get<AdminStats>('/admin/stats')
  return data
}
