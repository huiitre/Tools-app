import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { ApiKey, AdminApiKeyColumn, AdminSortDir, AdminApiKeyPageSize, ApiKeyStatusFilter } from '../types/adminApiKeys.types'

const INITIAL_COLUMNS: AdminApiKeyColumn[] = [
  { key: 'name',        label: 'Nom',          description: 'Nom de la clé',                    visible: true,  sortable: true,  userToggle: false, minSize: 140, maxSize: 220, grow: 1 },
  { key: 'description', label: 'Description',  description: 'À quoi sert la clé',               visible: true,  sortable: false, userToggle: true,  minSize: 160, maxSize: 320, grow: 1 },
  { key: 'prefix',      label: 'Préfixe',      description: 'Début de la clé, pour la reconnaître', visible: true, sortable: false, userToggle: true, minSize: 130, maxSize: 130, grow: 0 },
  { key: 'scopes',      label: 'Scopes',       description: 'Droits accordés à la clé',         visible: true,  sortable: false, userToggle: true,  minSize: 180, maxSize: 360, grow: 1 },
  { key: 'expiresAt',   label: 'Expiration',   description: "Fin de validité de la clé",        visible: true,  sortable: true,  userToggle: true,  minSize: 130, maxSize: 150, grow: 0 },
  { key: 'lastUsedAt',  label: 'Utilisation',  description: 'Dernière utilisation',             visible: true,  sortable: true,  userToggle: true,  minSize: 140, maxSize: 170, grow: 0 },
  { key: 'createdAt',   label: 'Création',     description: 'Date et auteur de la création',    visible: false, sortable: true,  userToggle: true,  minSize: 140, maxSize: 180, grow: 0 },
  { key: 'actions',     label: '',             description: '',                                 visible: true,  sortable: false, userToggle: false, minSize: 40,  maxSize: 40,  grow: 0 },
]

const time = (value: string | null) => (value ? new Date(value).getTime() : 0)

export const useAdminApiKeysStore = defineStore('adminApiKeys', () => {
  const apiKeys = ref<ApiKey[]>([])
  const scopes = ref<string[]>([])
  // Nom des créateurs, résolu à part : la clé ne porte que l'identifiant.
  const userNames = ref<Record<string, string>>({})
  const columns = ref<AdminApiKeyColumn[]>(INITIAL_COLUMNS.map(c => ({ ...c })))
  const loading = ref(false)
  const q = ref<string | null>(null)
  const sort = ref<string | null>(null)
  const dir = ref<AdminSortDir>('ASC')
  const page = ref(1)
  const pageSize = ref<AdminApiKeyPageSize>(20)
  const statusFilter = ref<ApiKeyStatusFilter>('active')

  const visibleColumns = computed(() => columns.value.filter(c => c.visible))

  const gridTemplateColumns = computed(() =>
    visibleColumns.value
      .flatMap(col => {
        const track = col.grow === 0 ? `${col.minSize}px` : `minmax(${col.minSize}px, ${col.maxSize}px)`
        return col.key === 'actions' ? ['1fr', track] : [track]
      })
      .join(' ')
  )

  // Par défaut, seulement les clés non révoquées ; les révoquées restent consultables par le filtre.
  const byStatus = computed(() => {
    if (statusFilter.value === 'active') return apiKeys.value.filter(k => !k.isRevoked)
    if (statusFilter.value === 'revoked') return apiKeys.value.filter(k => k.isRevoked)
    return apiKeys.value
  })

  const countByStatus = computed(() => {
    const revoked = apiKeys.value.filter(k => k.isRevoked).length
    return { active: apiKeys.value.length - revoked, revoked, all: apiKeys.value.length }
  })

  const filtered = computed(() => {
    let list = byStatus.value
    if (q.value) {
      const lq = q.value.toLowerCase()
      list = list.filter(k =>
        k.name.toLowerCase().includes(lq) ||
        k.description.toLowerCase().includes(lq) ||
        k.keyPrefix.toLowerCase().includes(lq) ||
        k.scopes.some(s => s.includes(lq))
      )
    }
    return list
  })

  // Sans tri choisi : l'ordre de l'API, la plus récente d'abord.
  const sorted = computed(() => {
    if (!sort.value) return filtered.value
    const key = sort.value
    return [...filtered.value].sort((a, b) => {
      let av: string | number = ''
      let bv: string | number = ''

      if (key === 'name') { av = a.name.toLowerCase(); bv = b.name.toLowerCase() }
      else if (key === 'expiresAt') { av = time(a.expiresAt); bv = time(b.expiresAt) }
      else if (key === 'lastUsedAt') { av = time(a.lastUsedAt); bv = time(b.lastUsedAt) }
      else if (key === 'createdAt') { av = time(a.createdAt); bv = time(b.createdAt) }

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

  function setStatusFilter(value: ApiKeyStatusFilter) {
    statusFilter.value = value
    page.value = 1
  }

  function setPage(p: number) { page.value = p }
  function setPageSize(s: AdminApiKeyPageSize) { pageSize.value = s; page.value = 1 }

  function toggleColumn(key: string) {
    const col = columns.value.find(c => c.key === key)
    if (col) col.visible = !col.visible
  }

  // La réponse de création porte la clé telle qu'en base : rien à recharger.
  function addLocally(apiKey: ApiKey) {
    apiKeys.value = [apiKey, ...apiKeys.value]
  }

  // La clé reste connue : elle passe simplement du filtre « actives » au filtre « révoquées ».
  function markRevokedLocally(id: number) {
    const now = new Date().toISOString()
    apiKeys.value = apiKeys.value.map(k => (k.id === id ? { ...k, isRevoked: true, revokedAt: now } : k))
    if (page.value > lastPage.value) page.value = lastPage.value
  }

  function creatorName(apiKey: ApiKey) {
    return userNames.value[String(apiKey.createdBy)] ?? `Utilisateur #${apiKey.createdBy}`
  }

  return {
    apiKeys, scopes, userNames, columns, loading,
    q, sort, dir, page, pageSize, statusFilter,
    visibleColumns, gridTemplateColumns,
    byStatus, countByStatus, filtered, sorted, paginated, total, lastPage,
    setQuery, setSort, toggleSort, setStatusFilter, setPage, setPageSize,
    toggleColumn, addLocally, markRevokedLocally, creatorName,
  }
})
