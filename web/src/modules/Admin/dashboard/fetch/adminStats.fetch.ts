import { clientV3 } from '@/services/axiosInstance'

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
  const { data } = await clientV3.get<AdminStats>('/admin/stats')
  return data
}
