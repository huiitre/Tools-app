import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { VpnPeer, VpnPeerStatus, AdminVpnColumn, AdminSortDir, AdminVpnPageSize } from '../types/adminVpn.types'

const INITIAL_COLUMNS: AdminVpnColumn[] = [
  { key: 'name',      label: 'Nom',        description: 'Nom du peer',                    visible: true,  sortable: true,  userToggle: false, minSize: 130, maxSize: 200, grow: 1 },
  { key: 'ip',        label: 'IP',         description: 'Adresse dans le tunnel',         visible: true,  sortable: true,  userToggle: true,  minSize: 110, maxSize: 130, grow: 0 },
  { key: 'status',    label: 'État',       description: 'Connectivité du tunnel',         visible: true,  sortable: true,  userToggle: true,  minSize: 140, maxSize: 160, grow: 0 },
  { key: 'handshake', label: 'Activité',   description: 'Dernière poignée de main',       visible: true,  sortable: true,  userToggle: true,  minSize: 150, maxSize: 180, grow: 0 },
  { key: 'rx',        label: 'Reçu',       description: 'Octets reçus depuis le démarrage',  visible: true,  sortable: true,  userToggle: true,  minSize: 100, maxSize: 120, grow: 0 },
  { key: 'tx',        label: 'Envoyé',     description: 'Octets envoyés depuis le démarrage', visible: true, sortable: true,  userToggle: true,  minSize: 100, maxSize: 120, grow: 0 },
  { key: 'valid',     label: 'Intégrité',  description: 'La configuration peut-elle fonctionner', visible: true, sortable: true, userToggle: true, minSize: 100, maxSize: 120, grow: 0 },
  { key: 'publicKey', label: 'Clé publique', description: 'Clé publique du peer',         visible: false, sortable: false, userToggle: true,  minSize: 200, maxSize: 340, grow: 1 },
  { key: 'actions',   label: '',           description: '',                               visible: true,  sortable: false, userToggle: false, minSize: 76,  maxSize: 76,  grow: 0 },
]

// Du plus sain au plus cassé : c'est l'ordre dans lequel on veut lire la colonne État.
const STATUS_RANK: Record<VpnPeerStatus, number> = {
  connected: 0,
  idle: 1,
  never: 2,
  orphan: 3,
}

// Une IP triée comme du texte place .10 avant .2 : seul le dernier octet fait foi.
const lastOctet = (ip: string) => Number(ip.split('.').pop() ?? 0)

export const useAdminVpnStore = defineStore('adminVpn', () => {
  const peers = ref<VpnPeer[]>([])
  const columns = ref<AdminVpnColumn[]>(INITIAL_COLUMNS.map(c => ({ ...c })))
  const loading = ref(false)
  const error = ref<string | null>(null)
  const q = ref<string | null>(null)
  const sort = ref<string | null>(null)
  const dir = ref<AdminSortDir>('ASC')
  const page = ref(1)
  const pageSize = ref<AdminVpnPageSize>(20)

  const visibleColumns = computed(() => columns.value.filter(c => c.visible))

  // Une piste souple juste avant les actions : la poubelle reste collée à droite quel que soit
  // le nombre de colonnes affichées.
  const gridTemplateColumns = computed(() =>
    visibleColumns.value
      .flatMap(col => {
        const track = col.grow === 0 ? `${col.minSize}px` : `minmax(${col.minSize}px, ${col.maxSize}px)`
        return col.key === 'actions' ? ['1fr', track] : [track]
      })
      .join(' ')
  )

  const filtered = computed(() => {
    let list = peers.value
    if (q.value) {
      const lq = q.value.toLowerCase()
      list = list.filter(p =>
        p.name.toLowerCase().includes(lq) ||
        p.ip.includes(lq)
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
      else if (key === 'ip') { av = lastOctet(a.ip); bv = lastOctet(b.ip) }
      else if (key === 'status') { av = STATUS_RANK[a.status]; bv = STATUS_RANK[b.status] }
      // Un peer jamais vu part au bout du tri croissant : c'est le plus ancien de tous.
      else if (key === 'handshake') {
        av = a.handshakeSecondsAgo ?? Number.MAX_SAFE_INTEGER
        bv = b.handshakeSecondsAgo ?? Number.MAX_SAFE_INTEGER
      }
      else if (key === 'rx') { av = a.rxBytes; bv = b.rxBytes }
      else if (key === 'tx') { av = a.txBytes; bv = b.txBytes }
      else if (key === 'valid') { av = a.valid ? 1 : 0; bv = b.valid ? 1 : 0 }

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
  function setPageSize(s: AdminVpnPageSize) { pageSize.value = s; page.value = 1 }

  function toggleColumn(key: string) {
    const col = columns.value.find(c => c.key === key)
    if (col) col.visible = !col.visible
  }

  // La suppression est définitive côté serveur : retirer la ligne suffit, rien à recharger.
  function removeLocally(name: string) {
    peers.value = peers.value.filter(p => p.name !== name)
    if (page.value > lastPage.value) page.value = lastPage.value
  }

  return {
    peers, columns, loading, error,
    q, sort, dir, page, pageSize,
    visibleColumns, gridTemplateColumns,
    filtered, sorted, paginated, total, lastPage,
    setQuery, setSort, toggleSort, setPage, setPageSize,
    toggleColumn, removeLocally,
  }
})
