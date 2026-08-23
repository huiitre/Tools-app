<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { useFloating, offset } from '@floating-ui/vue'
import { useAdminVpnStore } from '../store/adminVpn.store'
import { ADMIN_VPN_PAGE_SIZES } from '../types/adminVpn.types'

const emit = defineEmits<{ create: [] }>()

const store = useAdminVpnStore()

/* ── Search ────────────────────────────────────────────── */
const searchValue = ref(store.q ?? '')
let debounce: ReturnType<typeof setTimeout> | null = null

watch(searchValue, v => {
  if (debounce) clearTimeout(debounce)
  debounce = setTimeout(() => store.setQuery(v.trim() || null), 300)
})

/* ── Pagination ────────────────────────────────────────── */
const goFirst    = () => store.setPage(1)
const goPrevious = () => store.page > 1 && store.setPage(store.page - 1)
const goNext     = () => store.page < store.lastPage && store.setPage(store.page + 1)
const goLast     = () => store.setPage(store.lastPage)

/* ── Preferences panel ─────────────────────────────────── */
const isPreferencesOpen = ref(false)
const preferencesRef      = ref<HTMLElement | null>(null)
const preferencesFloating = ref<HTMLElement | null>(null)

const { floatingStyles } = useFloating(preferencesRef, preferencesFloating, {
  placement: 'bottom-start',
  middleware: [offset(6)],
})

const onClickOutside = (e: MouseEvent) => {
  const t = e.target as HTMLElement
  if (
    preferencesRef.value && preferencesFloating.value &&
    !preferencesRef.value.contains(t) && !preferencesFloating.value.contains(t)
  ) isPreferencesOpen.value = false
}

const onScroll = () => { isPreferencesOpen.value = false }

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('scroll', onScroll, true)
})
onBeforeUnmount(() => {
  if (debounce) clearTimeout(debounce)
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('scroll', onScroll, true)
})
</script>

<template>
  <div class="vpn-toolbar">
    <!-- LEFT : pagination + préférences -->
    <div class="toolbar-left">
      <button class="icon" :disabled="store.page === 1" @click="goFirst">
        <i class="mdi mdi-page-first" />
      </button>
      <button class="icon" :disabled="store.page === 1" @click="goPrevious">
        <i class="mdi mdi-chevron-left" />
      </button>

      <span class="page-indicator">
        Page <strong>{{ store.page }}</strong> / {{ store.lastPage }}
        <span class="total-hint">({{ store.total }} peers)</span>
      </span>

      <button class="icon" :disabled="store.page >= store.lastPage" @click="goNext">
        <i class="mdi mdi-chevron-right" />
      </button>
      <button class="icon" :disabled="store.page >= store.lastPage" @click="goLast">
        <i class="mdi mdi-page-last" />
      </button>

      <button ref="preferencesRef" class="icon pref-btn" @click="isPreferencesOpen = !isPreferencesOpen">
        <i class="mdi mdi-tune" />
      </button>

      <div v-if="isPreferencesOpen" ref="preferencesFloating" class="floating-panel" :style="floatingStyles">
        <div class="pref-panel">
          <h3 class="panel-title">Peers</h3>

          <div class="pref-block">
            <label class="pref-label">Taille de page</label>
            <select
              class="pref-select"
              :value="store.pageSize"
              @change="store.setPageSize(Number(($event.target as HTMLSelectElement).value) as any)"
            >
              <option v-for="s in ADMIN_VPN_PAGE_SIZES" :key="s" :value="s">{{ s }} lignes</option>
            </select>
          </div>

          <div class="pref-block">
            <label class="pref-label">Colonnes</label>
            <div v-for="col in store.columns.filter(c => c.userToggle)" :key="col.key" class="pref-switch">
              <span>{{ col.label }}</span>
              <label>
                <input type="checkbox" role="switch" :checked="col.visible" @change="store.toggleColumn(col.key)" />
              </label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- RIGHT : recherche + ajout -->
    <div class="toolbar-right">
      <input v-model="searchValue" type="search" class="search-input" placeholder="Rechercher un peer…" />
      <button class="btn-create" @click="emit('create')">
        <i class="mdi mdi-plus" /> Ajouter
      </button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.vpn-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.25rem 0;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

button {
  background: none;
  border: none;
  color: var(--pico-muted-color);
  cursor: pointer;
  &:disabled { opacity: 0.35; cursor: not-allowed; }
  &:hover:not(:disabled) { color: var(--pico-primary); }
}

button.icon {
  padding: 0.25rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  i { font-size: 1.1rem; }
}

.pref-btn { margin-left: 0.5rem; }

.page-indicator {
  margin: 0 0.5rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  user-select: none;
}

.total-hint {
  font-size: 0.78rem;
  opacity: 0.7;
  margin-left: 0.25rem;
}

.search-input {
  width: 220px;
  height: 2rem;
  margin: 0;
  font-size: 0.75rem;
}

.btn-create {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  width: auto;
  height: 2rem;
  padding: 0 0.7rem;
  margin: 0;
  border: 1px solid var(--pico-form-element-border-color);
  border-radius: var(--pico-border-radius);
  background: var(--pico-form-element-background-color);
  color: var(--pico-form-element-color);
  font-size: 0.75rem;
  white-space: nowrap;

  i { font-size: 0.9rem; }

  &:hover:not(:disabled) {
    border-color: var(--pico-primary);
    color: var(--pico-primary);
  }
}

/* ── Floating panel ──────────────────────────────────────── */
.floating-panel {
  position: absolute;
  z-index: 1000;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
}

.pref-panel {
  padding: 0.65rem 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  min-width: 220px;
  font-size: 0.75rem;
}

.panel-title {
  font-size: 1rem;
  font-weight: 600;
  margin: 0;
}

.pref-block {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  align-items: flex-start;
}

.pref-label {
  font-weight: 500;
  color: var(--pico-primary);
}

.pref-select {
  width: auto;
  min-width: 130px;
  height: 2rem;
  margin: 0;
  padding: 0 2rem 0 0.5rem;
  font-size: 0.75rem;
}

.pref-switch {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
}
</style>
