import type { RoleCode } from '@/modules/Auth/types/auth.types'

export type AppLogSortBy =
  | 'createdAt'
  | 'userName'
  | 'userEmail'
  | 'userRole'
  | 'userStatus'
  | 'userRegisteredAt'
  | 'moduleName'
  | 'areaCode'
  | 'actionCode'
  | 'ipAddress'
  | 'userAgent'
  | 'hasMetadata'

export type AppLogSortDirection = 'asc' | 'desc'

export type AppLogAdminItem = {
  id: number
  createdAt: string
  moduleId: number | null
  moduleName: string | null
  areaCode: string
  actionCode: string
  userId: number | null
  userName: string | null
  userEmail: string | null
  userRoleId: number | null
  userRoleCode: RoleCode | null
  userActive: boolean | null
  userRegisteredAt: string | null
  ipAddress: string | null
  userAgent: string | null
  hasMetadata: boolean
}

export type AppLogPage = {
  items: AppLogAdminItem[]
  totalCount: number
  page: number
  pageSize: number
}

export type AppLogListRequest = {
  page: number
  pageSize: number
  sortBy: AppLogSortBy
  sortDirection: AppLogSortDirection
}

export type AppLogColumn = {
  key: AppLogSortBy
  label: string
  description: string
  visible: boolean
  sortable: boolean
  userToggle: boolean
  minSize: number
  maxSize: number
  grow: number
}

export const APP_LOG_PAGE_SIZES = [10, 20, 50, 100] as const
export type AppLogPageSize = (typeof APP_LOG_PAGE_SIZES)[number]
