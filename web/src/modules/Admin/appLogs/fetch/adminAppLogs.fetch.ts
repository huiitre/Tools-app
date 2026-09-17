import { clientCore } from '@/services/axiosInstance'
import type { AppLogListRequest, AppLogPage } from '../types/adminAppLogs.types'

export async function fetchAdminAppLogs(query: AppLogListRequest): Promise<AppLogPage> {
  const { data } = await clientCore.get<AppLogPage>('/admin/app-logs', { params: query })
  return data
}
