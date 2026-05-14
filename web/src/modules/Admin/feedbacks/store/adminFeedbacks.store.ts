import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Feedback } from '@/modules/Core/Feedback/feedback.types'
import type { AdminFeedbackColumn, AdminSortDir, AdminFeedbackPageSize } from '../types/adminFeedbacks.types'

export const COLUMNS: AdminFeedbackColumn[] = [
  { key: 'userName',  label: 'Utilisateur', sortable: true,  minSize: 150, grow: 0 },
  { key: 'message',   label: 'Message',     sortable: false, minSize: 300, grow: 1 },
  { key: 'createdAt', label: 'Date',        sortable: true,  minSize: 140, grow: 0 },
  { key: 'isRead',    label: 'Lu',          sortable: true,  minSize: 60,  grow: 0 },
  { key: 'actions',  label: '',            sortable: false, minSize: 44,  grow: 0 },
]

const STORAGE_KEY_PAGE_SIZE = 'admin.feedbacks.page_size'
const STORAGE_KEY_SHOW_READ = 'admin.feedbacks.show_read'

function readPageSize(): AdminFeedbackPageSize {
  const raw = localStorage.getItem(STORAGE_KEY_PAGE_SIZE)
  return raw ? (parseInt(raw, 10) as AdminFeedbackPageSize) : 20
}

function readShowRead(): boolean {
  return localStorage.getItem(STORAGE_KEY_SHOW_READ) === 'true'
}

export const useAdminFeedbacksStore = defineStore('adminFeedbacks', () => {
  const feedbacks = ref<Feedback[]>([])
  const loading   = ref(false)
  const q         = ref<string | null>(null)
  const sort      = ref<string | null>(null)
  const dir       = ref<AdminSortDir>('ASC')
  const page      = ref(1)
  const pageSize  = ref<AdminFeedbackPageSize>(readPageSize())
  const showRead  = ref(readShowRead())

  const gridTemplateColumns = computed(() =>
    COLUMNS.map(col => col.grow === 0 ? `${col.minSize}px` : `minmax(${col.minSize}px, 1fr)`).join(' ')
  )

  const filtered = computed(() => {
    let list = showRead.value ? feedbacks.value : feedbacks.value.filter(f => !f.isRead)
    if (q.value) {
      const lq = q.value.toLowerCase()
      list = list.filter(f =>
        f.userName.toLowerCase().includes(lq) ||
        f.message.toLowerCase().includes(lq)
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
      if (key === 'userName')  { av = a.userName;  bv = b.userName }
      if (key === 'createdAt') { av = a.createdAt; bv = b.createdAt }
      if (key === 'isRead')    { av = a.isRead ? 1 : 0; bv = b.isRead ? 1 : 0 }
      if (av < bv) return dir.value === 'ASC' ? -1 : 1
      if (av > bv) return dir.value === 'ASC' ? 1 : -1
      return 0
    })
  })

  const total    = computed(() => filtered.value.length)
  const lastPage = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))
  const paginated = computed(() => {
    const start = (page.value - 1) * pageSize.value
    return sorted.value.slice(start, start + pageSize.value)
  })

  function setQuery(value: string | null) { q.value = value; page.value = 1 }

  function toggleSort(key: string) {
    if (sort.value !== key) { sort.value = key; dir.value = 'ASC' }
    else if (dir.value === 'ASC') dir.value = 'DESC'
    else { sort.value = null; dir.value = 'ASC' }
  }

  function setPage(p: number) { page.value = p }

  function setPageSize(s: AdminFeedbackPageSize) {
    pageSize.value = s
    page.value = 1
    localStorage.setItem(STORAGE_KEY_PAGE_SIZE, String(s))
  }

  function toggleShowRead() {
    showRead.value = !showRead.value
    page.value = 1
    localStorage.setItem(STORAGE_KEY_SHOW_READ, String(showRead.value))
  }

  function markReadLocally(id: number, isRead: boolean) {
    const fb = feedbacks.value.find(f => f.id === id)
    if (fb) fb.isRead = isRead
  }

  function removeLocally(id: number) {
    feedbacks.value = feedbacks.value.filter(f => f.id !== id)
  }

  return {
    feedbacks, loading, q, sort, dir, page, pageSize, showRead,
    gridTemplateColumns, filtered, sorted, paginated, total, lastPage,
    setQuery, toggleSort, setPage, setPageSize, toggleShowRead, markReadLocally, removeLocally,
  }
})
