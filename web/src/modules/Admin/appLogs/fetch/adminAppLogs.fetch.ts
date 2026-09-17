import { clientCore } from '@/services/axiosInstance'
import type { AppLogListRequest, AppLogPage } from '../types/adminAppLogs.types'

export async function fetchAdminAppLogs(query: AppLogListRequest): Promise<AppLogPage> {
  const { data } = await clientCore.get<AppLogPage>('/admin/app-logs', {
    params: query,
    // ASP.NET Core lie naturellement `userIds=1&userIds=2`, pas la notation `userIds[]=1`.
    paramsSerializer: { indexes: null },
  })
  return data
}
