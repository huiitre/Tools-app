export type AdminUserColumn = {
  key: string
  label: string
  description: string
  visible: boolean
  sortable: boolean
  userToggle: boolean
  minSize: number
  maxSize: number
  grow: number
}

export type AdminSortDir = 'ASC' | 'DESC'

export const ADMIN_PAGE_SIZES = [10, 20, 50, 100] as const
export type AdminPageSize = (typeof ADMIN_PAGE_SIZES)[number]

export type AdminRole = {
  id: number
  code: string
  name: string
  description: string
  active: boolean
}

export type AdminUser = {
  id: string
  email: string
  name: string
  active: boolean
  createdAt: string
  avatarUrl: string | null
  // Identifiant du rôle global, nul si l'utilisateur n'en a aucun. L'API n'envoie qu'un
  // identifiant : le catalogue des rôles est chargé à part par GET /roles.
  roleId: number | null
}
