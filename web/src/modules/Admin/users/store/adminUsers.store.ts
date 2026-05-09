import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { AdminUser, AdminRole, AdminUserColumn, AdminSortDir, AdminPageSize } from '../types/adminUsers.types'

const ROLE_HIERARCHY = ['READ_ONLY', 'USER', 'MODERATOR', 'ADMIN', 'TECH', 'OWNER']

const AVATAR_WIDTH = 36

const INITIAL_COLUMNS: AdminUserColumn[] = [
  { key: 'name',      label: 'Nom',         description: 'Nom de l\'utilisateur', visible: true,  sortable: true, userToggle: false, minSize: 140, maxSize: 220, grow: 1 },
  { key: 'email',     label: 'Email',        description: 'Adresse email',         visible: true,  sortable: true, userToggle: true,  minSize: 180, maxSize: 320, grow: 2 },
  { key: 'role',      label: 'Rôle',         description: 'Rôle global',           visible: true,  sortable: true, userToggle: true,  minSize: 110, maxSize: 150, grow: 0 },
  { key: 'active',    label: 'Statut',       description: 'Compte actif ou non',   visible: true,  sortable: true, userToggle: true,  minSize: 80,  maxSize: 100, grow: 0 },
  { key: 'createdAt', label: 'Inscription',  description: 'Date d\'inscription',   visible: true,  sortable: true, userToggle: true,  minSize: 110, maxSize: 140, grow: 0 },
]

export function getTopRoleCode(roleCodes: string[]): string | null {
  if (!roleCodes?.length) return null
  return roleCodes.reduce((best, code) =>
    ROLE_HIERARCHY.indexOf(code) > ROLE_HIERARCHY.indexOf(best) ? code : best
  )
}

export const useAdminUsersStore = defineStore('adminUsers', () => {
  const users = ref<AdminUser[]>([])
  const roles = ref<AdminRole[]>([])
  const columns = ref<AdminUserColumn[]>(INITIAL_COLUMNS.map(c => ({ ...c })))
  const loading = ref(false)
  const error = ref<string | null>(null)
  const q = ref<string | null>(null)
  const sort = ref<string | null>(null)
  const dir = ref<AdminSortDir>('ASC')
  const page = ref(1)
  const pageSize = ref<AdminPageSize>(20)

  const visibleColumns = computed(() => columns.value.filter(c => c.visible))

  const gridTemplateColumns = computed(() => {
    const dynamic = visibleColumns.value.map(col => {
      if (col.grow === 0) return `${col.minSize}px`
      return `minmax(${col.minSize}px, ${col.maxSize}px)`
    })
    return [`${AVATAR_WIDTH}px`, ...dynamic].join(' ')
  })

  const filtered = computed(() => {
    let list = users.value
    if (q.value) {
      const lq = q.value.toLowerCase()
      list = list.filter(u =>
        u.name.toLowerCase().includes(lq) ||
        u.email.toLowerCase().includes(lq)
      )
    }
    return list
  })

  const sorted = computed(() => {
    if (!sort.value) return filtered.value
    const key = sort.value
    return [...filtered.value].sort((a, b) => {
      let av: string | number = ''
      let bv: string | number = ''

      if (key === 'name') { av = a.name; bv = b.name }
      else if (key === 'email') { av = a.email; bv = b.email }
      else if (key === 'active') { av = a.active ? 1 : 0; bv = b.active ? 1 : 0 }
      else if (key === 'createdAt') { av = a.createdAt; bv = b.createdAt }
      else if (key === 'role') {
        av = ROLE_HIERARCHY.indexOf(getTopRoleCode(a.roles) ?? '')
        bv = ROLE_HIERARCHY.indexOf(getTopRoleCode(b.roles) ?? '')
      }

      if (av < bv) return dir.value === 'ASC' ? -1 : 1
      if (av > bv) return dir.value === 'ASC' ? 1 : -1
      return 0
    })
  })

  const total = computed(() => filtered.value.length)
  const lastPage = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))

  const paginated = computed(() => {
    const start = (page.value - 1) * pageSize.value
    return sorted.value.slice(start, start + pageSize.value)
  })

  function setQuery(value: string | null) {
    q.value = value
    page.value = 1
  }

  function setSort(key: string | null, d: AdminSortDir) {
    sort.value = key
    dir.value = d
  }

  function toggleSort(key: string) {
    if (sort.value !== key) setSort(key, 'ASC')
    else if (dir.value === 'ASC') setSort(key, 'DESC')
    else setSort(null, 'ASC')
  }

  function setPage(p: number) { page.value = p }
  function setPageSize(s: AdminPageSize) { pageSize.value = s; page.value = 1 }

  function toggleColumn(key: string) {
    const col = columns.value.find(c => c.key === key)
    if (col) col.visible = !col.visible
  }

  const editingRoleUserId = ref<string | null>(null)

  function openRoleEdit(userId: string) { editingRoleUserId.value = userId }
  function closeRoleEdit() { editingRoleUserId.value = null }

  function updateUserRoleLocally(userId: string, roleCode: string) {
    const user = users.value.find(u => u.id === userId)
    if (user) user.roles = [roleCode]
  }

  return {
    users, roles, columns, loading, error,
    q, sort, dir, page, pageSize,
    visibleColumns, gridTemplateColumns,
    filtered, sorted, paginated, total, lastPage,
    editingRoleUserId,
    setQuery, setSort, toggleSort, setPage, setPageSize,
    toggleColumn, openRoleEdit, closeRoleEdit, updateUserRoleLocally,
  }
})
