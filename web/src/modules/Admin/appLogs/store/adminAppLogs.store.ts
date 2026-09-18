import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchAdminAppLogs } from '../fetch/adminAppLogs.fetch'
import type {
  AppLogAdminItem,
  AppLogColumn,
  AppLogListRequest,
  AppLogPageSize,
  AppLogSortBy,
  AppLogSortDirection,
} from '../types/adminAppLogs.types'
import type { AdminRole } from '../../users/types/adminUsers.types'
import type { AdminUser } from '../../users/types/adminUsers.types'
import type { AdminModule } from '../../modules/types/adminModules.types'

const INITIAL_COLUMNS: AppLogColumn[] = [
  { key: 'userName',         label: 'Nom',         description: 'Nom de l’utilisateur',       visible: true, sortable: true, userToggle: false, minSize: 150, maxSize: 240, grow: 1 },
  { key: 'userEmail',        label: 'Email',       description: 'Adresse email',               visible: true, sortable: true, userToggle: true,  minSize: 210, maxSize: 320, grow: 1 },
  { key: 'userRole',         label: 'Rôle',        description: 'Rôle global',                 visible: true, sortable: true, userToggle: true,  minSize: 110, maxSize: 140, grow: 0 },
  { key: 'userStatus',       label: 'Statut',      description: 'Statut du compte',            visible: true, sortable: true, userToggle: true,  minSize: 95,  maxSize: 105, grow: 0 },
  { key: 'userRegisteredAt', label: 'Inscription', description: 'Date d’inscription',          visible: true, sortable: true, userToggle: true,  minSize: 125, maxSize: 140, grow: 0 },
  { key: 'createdAt',        label: 'Date du log', description: 'Date de création du log',     visible: true, sortable: true, userToggle: true,  minSize: 145, maxSize: 160, grow: 0 },
  { key: 'moduleName',       label: 'Module',      description: 'Module concerné',             visible: true, sortable: true, userToggle: true,  minSize: 130, maxSize: 180, grow: 1 },
  { key: 'areaCode',         label: 'Zone',        description: 'Zone fonctionnelle',          visible: true, sortable: true, userToggle: true,  minSize: 105, maxSize: 125, grow: 0 },
  { key: 'actionCode',       label: 'Action',      description: 'Action effectuée',            visible: true, sortable: true, userToggle: true,  minSize: 115, maxSize: 135, grow: 0 },
  { key: 'ipAddress',        label: 'Adresse IP',  description: 'Adresse IP du client',        visible: true, sortable: true, userToggle: true,  minSize: 130, maxSize: 150, grow: 0 },
  { key: 'userAgent',        label: 'User-agent',  description: 'Agent utilisateur du client', visible: true, sortable: true, userToggle: true,  minSize: 180, maxSize: 320, grow: 1 },
  { key: 'hasMetadata',      label: 'Métadonnées', description: 'Présence de métadonnées',     visible: true, sortable: true, userToggle: true,  minSize: 95,  maxSize: 105, grow: 0 },
]

export const useAdminAppLogsStore = defineStore('adminAppLogs', () => {
  const items = ref<AppLogAdminItem[]>([])
  const roles = ref<AdminRole[]>([])
  const users = ref<AdminUser[]>([])
  const modules = ref<AdminModule[]>([])
  const areaCodes = ref<string[]>([])
  const actionCodes = ref<string[]>([])
  const columns = ref<AppLogColumn[]>(INITIAL_COLUMNS.map(column => ({ ...column })))
  const totalCount = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const page = ref(1)
  const pageSize = ref<AppLogPageSize>(20)
  const sortBy = ref<AppLogSortBy>('createdAt')
  const sortDirection = ref<AppLogSortDirection>('desc')
  const userIds = ref<number[]>([])
  const moduleIds = ref<number[]>([])
  const selectedAreaCodes = ref<string[]>([])
  const selectedActionCodes = ref<string[]>([])
  const createdFrom = ref<string | null>(null)
  const createdTo = ref<string | null>(null)
  const search = ref<string | null>(null)
  let currentRequest = 0

  const lastPage = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))
  const visibleColumns = computed(() => columns.value.filter(column => column.visible))
  const gridTemplateColumns = computed(() =>
    visibleColumns.value
      .map(column => column.grow === 0 ? `${column.minSize}px` : `minmax(${column.minSize}px, ${column.maxSize}px)`)
      .join(' '),
  )
  const roleOf = computed(() => (roleId: number | null) =>
    roleId === null ? null : roles.value.find(role => role.id === roleId) ?? null,
  )

  function buildRequest(): AppLogListRequest {
    return {
      page: page.value,
      pageSize: pageSize.value,
      sortBy: sortBy.value,
      sortDirection: sortDirection.value,
      ...(userIds.value.length ? { userIds: userIds.value } : {}),
      ...(moduleIds.value.length ? { moduleIds: moduleIds.value } : {}),
      ...(selectedAreaCodes.value.length ? { areaCodes: selectedAreaCodes.value } : {}),
      ...(selectedActionCodes.value.length ? { actionCodes: selectedActionCodes.value } : {}),
      ...(createdFrom.value ? { createdFrom: createdFrom.value } : {}),
      ...(createdTo.value ? { createdTo: createdTo.value } : {}),
      ...(search.value ? { search: search.value } : {}),
    }
  }

  async function load() {
    const request = ++currentRequest
    loading.value = true
    error.value = null
    try {
      const result = await fetchAdminAppLogs(buildRequest())
      if (request !== currentRequest) return
      items.value = result.items
      totalCount.value = result.totalCount
      page.value = result.page
      areaCodes.value = result.filterOptions.areaCodes
      actionCodes.value = result.filterOptions.actionCodes
    } catch {
      if (request !== currentRequest) return
      items.value = []
      totalCount.value = 0
      error.value = 'Impossible de charger le journal applicatif.'
    } finally {
      if (request === currentRequest) loading.value = false
    }
  }

  async function setPage(nextPage: number) {
    if (nextPage < 1 || nextPage > lastPage.value || nextPage === page.value) return
    page.value = nextPage
    await load()
  }

  async function setPageSize(nextPageSize: AppLogPageSize) {
    if (nextPageSize === pageSize.value) return
    pageSize.value = nextPageSize
    page.value = 1
    await load()
  }

  async function toggleSort(column: AppLogSortBy) {
    if (sortBy.value === column) {
      sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortBy.value = column
      sortDirection.value = 'asc'
    }
    page.value = 1
    await load()
  }

  function toggleColumn(key: AppLogSortBy) {
    const column = columns.value.find(column => column.key === key)
    if (column?.userToggle) column.visible = !column.visible
  }

  // Toute mutation de filtre revient à la première page puis recharge : centralisé ici pour ne pas
  // répéter ces deux lignes dans chaque setter.
  async function applyFilter(mutate: () => void) {
    mutate()
    page.value = 1
    await load()
  }

  const setUserIds = (nextUserIds: number[]) => applyFilter(() => { userIds.value = nextUserIds })
  const setModuleIds = (nextModuleIds: number[]) => applyFilter(() => { moduleIds.value = nextModuleIds })
  const setAreaCodes = (nextAreaCodes: string[]) => applyFilter(() => { selectedAreaCodes.value = nextAreaCodes })
  const setActionCodes = (nextActionCodes: string[]) => applyFilter(() => { selectedActionCodes.value = nextActionCodes })
  const setCreatedFrom = (value: string | null) => applyFilter(() => { createdFrom.value = value })
  const setCreatedTo = (value: string | null) => applyFilter(() => { createdTo.value = value })
  const setDateRange = (from: string | null, to: string | null) => applyFilter(() => {
    createdFrom.value = from
    createdTo.value = to
  })
  const setSearch = (value: string | null) => applyFilter(() => { search.value = value })

  const clearFilters = () => applyFilter(() => {
    userIds.value = []
    moduleIds.value = []
    selectedAreaCodes.value = []
    selectedActionCodes.value = []
    createdFrom.value = null
    createdTo.value = null
    search.value = null
  })

  const hasFilters = computed(() => userIds.value.length > 0 || moduleIds.value.length > 0
    || selectedAreaCodes.value.length > 0 || selectedActionCodes.value.length > 0
    || createdFrom.value !== null || createdTo.value !== null || search.value !== null)

  return {
    items, roles, users, modules, areaCodes, actionCodes, columns, totalCount, loading, error, page, pageSize, sortBy, sortDirection,
    userIds, moduleIds, selectedAreaCodes, selectedActionCodes, createdFrom, createdTo, search, hasFilters,
    visibleColumns, lastPage, gridTemplateColumns, roleOf,
    load, setPage, setPageSize, toggleSort, toggleColumn,
    setUserIds, setModuleIds, setAreaCodes, setActionCodes, setCreatedFrom, setCreatedTo, setDateRange, setSearch, clearFilters,
  }
})
