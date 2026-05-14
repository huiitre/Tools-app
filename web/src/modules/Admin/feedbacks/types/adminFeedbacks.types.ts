export type AdminFeedbackColumn = {
  key: string
  label: string
  sortable: boolean
  minSize: number
  grow: number
}

export type AdminSortDir = 'ASC' | 'DESC'

export const ADMIN_FEEDBACK_PAGE_SIZES = [10, 20, 50, 100] as const
export type AdminFeedbackPageSize = (typeof ADMIN_FEEDBACK_PAGE_SIZES)[number]
