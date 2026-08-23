<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useR2rStore } from '../store/r2r.store'
import SystemCard from './SystemCard.vue'
import type { R2rSystem } from '../types/r2r.types'
import toast from '@/services/toast'

const store = useR2rStore()

const expedition   = computed(() => store.currentExpedition!)
const routeData    = computed(() => expedition.value?.parsedRouteData)
const params       = computed(() => routeData.value?.parameters ?? {})

const allSystems   = computed<R2rSystem[]>(() =>
  (routeData.value?.result ?? []).filter(s => s.bodies?.length > 0)
)

const curIdx       = computed(() => expedition.value?.currentSystemIndex ?? 0)
const curBodies    = computed(() => expedition.value?.currentBodiesDone ?? [])

/* ── FILTER ─────────────────────────────────────────────── */
const searchQuery = ref('')
const hideDone    = ref(localStorage.getItem('r2r_hide_done') === 'true')
watch(hideDone, val => localStorage.setItem('r2r_hide_done', String(val)))

const filteredSystems = computed(() => {
  const q    = searchQuery.value.toLowerCase().trim()
  const hide = hideDone.value
  return allSystems.value.filter((sys, idx) => {
    if (hide && idx < curIdx.value) return false
    if (q) return sys.name.toLowerCase().includes(q)
            || sys.bodies.some(b => b.name.toLowerCase().includes(q))
    return true
  })
})

/* ── PAGINATION ─────────────────────────────────────────── */
const PAGE       = 50
const rendered   = ref(PAGE)
const visible    = computed(() => filteredSystems.value.slice(0, rendered.value))
const hasMore    = computed(() => rendered.value < filteredSystems.value.length)
const sentinelEl = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null

function setupObserver() {
  observer?.disconnect()
  if (!hasMore.value) return
  nextTick(() => {
    if (!sentinelEl.value) return
    observer = new IntersectionObserver(entries => {
      if (!entries[0].isIntersecting) return
      rendered.value += PAGE
      nextTick(setupObserver)
    }, { rootMargin: '300px' })
    observer.observe(sentinelEl.value)
  })
}
onMounted(setupObserver)
onBeforeUnmount(() => observer?.disconnect())

/* ── MODE DÉTECTION ─────────────────────────────────────── */
const isExobio = computed(() =>
  allSystems.value.some(s => s.bodies.some(b => b.landmarks && b.landmarks.length > 0))
)

/* ── STATS ──────────────────────────────────────────────── */
const allBodies = computed(() => allSystems.value.flatMap(s => s.bodies))

function bodyBioValue(b: import('../types/r2r.types').R2rBody): number {
  return b.landmarks?.reduce((a, l) => a + l.value, 0) ?? 0
}

const totalMap = computed(() =>
  isExobio.value
    ? allBodies.value.reduce((a, b) => a + bodyBioValue(b), 0)
    : allBodies.value.reduce((a, b) => a + (b.estimated_mapping_value || 0), 0)
)
const remainingMap = computed(() => {
  let rem = 0
  allSystems.value.forEach((sys, idx) => {
    if (idx < curIdx.value) return
    sys.bodies.forEach(b => {
      if (idx === curIdx.value && curBodies.value.includes(b.id64)) return
      rem += isExobio.value ? bodyBioValue(b) : (b.estimated_mapping_value || 0)
    })
  })
  return rem
})

const totalJumps     = computed(() => allSystems.value.reduce((a, s) => a + (s.jumps || 0), 0))
const remainingJumps = computed(() => allSystems.value.slice(curIdx.value).reduce((a, s) => a + (s.jumps || 0), 0))
const wwCount     = computed(() => allBodies.value.filter(b => b.subtype?.toLowerCase().includes('water')).length)
const hmcCount    = computed(() => allBodies.value.filter(b => b.subtype?.toLowerCase().includes('high metal')).length)
const terraCount  = computed(() => allBodies.value.filter(b => b.is_terraformable).length)
const progress    = computed(() => allSystems.value.length ? Math.round(curIdx.value / allSystems.value.length * 100) : 0)

const totalBioSites = computed(() =>
  allBodies.value.reduce((a, b) =>
    a + (b.landmarks?.reduce((s, l) => s + l.count, 0) || 0), 0)
)
const bioTypeCounts = computed(() => {
  const counts: Record<string, number> = {}
  allBodies.value.forEach(b =>
    b.landmarks?.forEach(l => { counts[l.type] = (counts[l.type] || 0) + l.count })
  )
  return counts
})

/* ── FORMAT ─────────────────────────────────────────────── */
function formatCr(v: number) {
  if (!v) return '—'
  if (v >= 1_000_000) {
    const [int, dec] = (v / 1_000_000).toFixed(2).split('.')
    const intFmt = int.replace(/\B(?=(\d{3})+(?!\d))/g, ' ')
    return `${intFmt}.${dec} MCr`
  }
  if (v >= 1_000) return Math.round(v / 1_000) + ' kCr'
  return v + ' Cr'
}

/* ── SAVE ───────────────────────────────────────────────── */
let saveTimer: ReturnType<typeof setTimeout> | null = null
function scheduleSave(sysIdx: number, bodies: number[]) {
  if (saveTimer) clearTimeout(saveTimer)
  saveTimer = setTimeout(async () => {
    try { await store.saveProgress(sysIdx, bodies) }
    catch { toast.error('Erreur lors de la sauvegarde') }
  }, 350)
}

/* ── INTERACTIONS ───────────────────────────────────────── */
function onBodyToggle(sysIndex: number, bodyId: number, checked: boolean) {
  if (sysIndex !== curIdx.value) return
  const bodies = [...curBodies.value]
  let nextIdx  = curIdx.value

  if (checked) {
    if (!bodies.includes(bodyId)) bodies.push(bodyId)
    const sys     = allSystems.value[nextIdx]
    const allDone = sys?.bodies.every(b => bodies.includes(b.id64))
    if (allDone) { scheduleSave(nextIdx + 1, []); return }
  } else {
    const i = bodies.indexOf(bodyId)
    if (i !== -1) bodies.splice(i, 1)
  }
  scheduleSave(nextIdx, bodies)
}

function onSystemDone(sysIndex: number) {
  scheduleSave(sysIndex + 1, [])
}

async function onRevertTo(sysIndex: number) {
  try {
    await store.saveProgress(sysIndex, [])
    toast.success(`Progression reprise au système #${sysIndex + 1}`)
  } catch {
    toast.error('Erreur lors de la mise à jour')
  }
}

/* ── COPY ───────────────────────────────────────────────── */
async function copy(text: string) {
  if (!text || text === '?') return
  try { await navigator.clipboard.writeText(text) }
  catch {
    const ta = document.createElement('textarea')
    ta.value = text; ta.style.cssText = 'position:fixed;opacity:0'
    document.body.appendChild(ta); ta.select()
    document.execCommand('copy'); document.body.removeChild(ta)
  }
  toast.success(`Copié : ${text}`)
}

/* ── ACTIONS ────────────────────────────────────────────── */
async function doExport() {
  const name = expedition.value.name.replace(/[^a-z0-9]/gi, '_').toLowerCase()
  await store.exportExpedition(expedition.value.id, `${name}.json`)
}

async function doRename() {
  const n = prompt('Nouveau nom :', expedition.value.name)
  if (n?.trim()) await store.renameExpedition(expedition.value.id, n.trim())
}
</script>

<template>
  <div class="tracker">

    <!-- ── HEADER ─────────────────────────────────────── -->
    <div class="tracker-header">
      <div class="tracker-title">◈ {{ expedition.name }}</div>

      <div class="tracker-route">
        <span
          class="route-node copyable"
          :title="'Copier : ' + (params.source || '')"
          @click="copy(params.source as string || '')"
        >{{ params.source || '?' }}</span>
        <span class="route-arrow">→</span>
        <span
          class="route-node copyable"
          :title="'Copier : ' + (params.destination || '')"
          @click="copy(params.destination as string || '')"
        >{{ params.destination || '?' }}</span>
      </div>

      <div class="tracker-actions">
        <button class="outline" @click="doRename">Renommer</button>
        <button class="outline" @click="doExport">Exporter JSON</button>
        <button class="outline secondary" @click="store.closeExpedition()">← Retour</button>
      </div>
    </div>

    <!-- ── PROGRESS ───────────────────────────────────── -->
    <div class="tracker-progress">
      <progress :value="progress" max="100" />
      <span class="progress-label">
        <strong>{{ curIdx }}</strong> / {{ allSystems.length }} systèmes &nbsp;·&nbsp; {{ progress }}%
      </span>
    </div>

    <!-- ── STATS ──────────────────────────────────────── -->
    <div class="stats-panel">
      <div class="stats-group">
        <div class="stat-card">
          <span class="stat-lbl">Systèmes</span>
          <span class="stat-val c-primary">{{ allSystems.length }}</span>
        </div>
        <div class="stat-card">
          <span class="stat-lbl">Corps</span>
          <span class="stat-val c-blue">{{ allBodies.length }}</span>
        </div>
        <div class="stat-card">
          <span class="stat-lbl">Sauts totaux</span>
          <span class="stat-val">{{ totalJumps }}</span>
        </div>
        <div class="stat-card">
          <span class="stat-lbl">Sauts restants</span>
          <span class="stat-val c-primary">{{ remainingJumps }}</span>
        </div>
      </div>

      <template v-if="!isExobio">
        <div class="stats-group">
          <div class="stat-card">
            <span class="stat-lbl">Water Worlds</span>
            <span class="stat-val c-blue">{{ wwCount }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-lbl">HMC</span>
            <span class="stat-val c-orange">{{ hmcCount }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-lbl">Terraformables</span>
            <span class="stat-val c-green">{{ terraCount }}</span>
          </div>
        </div>

        <div class="stats-group">
          <div class="stat-card">
            <span class="stat-lbl">Val. scan totale</span>
            <span class="stat-val">{{ formatCr(allBodies.reduce((a, b) => a + (b.estimated_scan_value || 0), 0)) }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-lbl">Val. map totale</span>
            <span class="stat-val c-green">{{ formatCr(totalMap) }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-lbl">Val. restante</span>
            <span class="stat-val c-primary">{{ formatCr(remainingMap) }}</span>
          </div>
        </div>
      </template>

      <template v-else>
        <div class="stats-group">
          <div class="stat-card">
            <span class="stat-lbl">Sites bio</span>
            <span class="stat-val c-green">{{ totalBioSites }}</span>
          </div>
          <div
            v-for="(count, type) in bioTypeCounts"
            :key="type"
            class="stat-card"
          >
            <span class="stat-lbl">{{ type }}</span>
            <span class="stat-val c-green">{{ count }}</span>
          </div>
        </div>

        <div class="stats-group">
          <div class="stat-card">
            <span class="stat-lbl">Val. bio totale</span>
            <span class="stat-val c-green">{{ formatCr(totalMap) }}</span>
          </div>
          <div class="stat-card">
            <span class="stat-lbl">Val. restante</span>
            <span class="stat-val c-primary">{{ formatCr(remainingMap) }}</span>
          </div>
        </div>
      </template>

      <div v-if="params.range || params.min_value || params.radius" class="stats-group">
        <div v-if="params.range" class="stat-card">
          <span class="stat-lbl">Portée</span>
          <span class="stat-val">{{ params.range }} al</span>
        </div>
        <div v-if="params.min_value" class="stat-card">
          <span class="stat-lbl">Val. min</span>
          <span class="stat-val">{{ formatCr(Number(params.min_value)) }}</span>
        </div>
        <div v-if="params.radius" class="stat-card">
          <span class="stat-lbl">Rayon</span>
          <span class="stat-val">{{ params.radius }} al</span>
        </div>
      </div>
    </div>

    <!-- ── TOOLBAR ─────────────────────────────────────── -->
    <div class="tracker-toolbar">
      <input
        v-model="searchQuery"
        class="search-input"
        type="text"
        placeholder="Rechercher un système ou une planète…"
      />
      <label class="filter-label">
        <input type="checkbox" v-model="hideDone" />
        Masquer les systèmes faits
      </label>
    </div>

    <!-- ── SYSTEMS ─────────────────────────────────────── -->
    <div class="systems-list">
      <SystemCard
        v-for="sys in visible"
        :key="sys.id64"
        :system="sys"
        :system-index="allSystems.indexOf(sys)"
        :current-system-index="curIdx"
        :current-bodies-done="curBodies"
        :is-exobio="isExobio"
        @body-toggle="(bodyId, checked) => onBodyToggle(allSystems.indexOf(sys), bodyId, checked)"
        @system-done="onSystemDone(allSystems.indexOf(sys))"
        @revert-to="onRevertTo(allSystems.indexOf(sys))"
      />
      <div v-if="hasMore" ref="sentinelEl" style="height:1px" />
    </div>

    <!-- ── FOOTER ──────────────────────────────────────── -->
    <div class="tracker-footer">
      <span>{{ curIdx }} / {{ allSystems.length }} systèmes</span>
      <span>{{ allBodies.length }} corps</span>
      <span v-if="!isExobio">Val. map : <strong>{{ formatCr(totalMap) }}</strong></span>
      <span v-else>Val. bio : <strong>{{ formatCr(totalMap) }}</strong></span>
      <span>Restante : <strong class="c-primary">{{ formatCr(remainingMap) }}</strong></span>
    </div>

  </div>
</template>

<style lang="scss" scoped>
.tracker {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

/* ── HEADER ─── */
.tracker-header {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  padding: 0.65rem 1.5rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-wrap: wrap;
}

.tracker-title {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--pico-primary);
  flex-shrink: 0;
  letter-spacing: 0.04em;
}

.tracker-route {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
}

.route-node {
  &.copyable {
    cursor: pointer;
    color: var(--pico-color);
    transition: color 0.15s;
    &:hover { color: var(--pico-primary); }
  }
}

.route-arrow { color: var(--pico-primary); font-size: 1rem; }

.tracker-actions {
  display: flex;
  gap: 0.5rem;
  flex-shrink: 0;

  button {
    padding: 0.3rem 0.75rem;
    font-size: 0.8rem;
    margin: 0;
  }
}

/* ── PROGRESS ─── */
.tracker-progress {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.5rem 1.5rem;
  border-bottom: 1px solid var(--pico-muted-border-color);

  progress { flex: 1; margin: 0; height: 6px; }
}

.progress-label {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
  white-space: nowrap;
  strong { color: var(--pico-primary); }
}

/* ── STATS ─── */
.stats-panel {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 0;
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
}

.stats-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0 0.75rem;

  &:first-child { padding-left: 0; }

  & + & { border-left: 1px solid var(--pico-muted-border-color); }
}

.stat-card {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 75px;
  padding: 0.4rem 0.65rem;
  background: var(--pico-card-sectioning-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
}

.stat-lbl {
  font-size: 0.62rem;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  white-space: nowrap;
}

.stat-val {
  font-size: 1rem;
  font-weight: 700;
  white-space: nowrap;
  color: var(--pico-color);
}

.c-primary { color: var(--pico-primary) !important; }
.c-blue    { color: #3ea6e0 !important; }
.c-orange  { color: #c87820 !important; }
.c-green   { color: #3ecf8e !important; }

/* ── TOOLBAR ─── */
.tracker-toolbar {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.6rem 1.5rem;
  border-bottom: 1px solid var(--pico-muted-border-color);
  flex-wrap: wrap;
}

.search-input {
  width: 280px;
  padding: 0.35rem 0.75rem;
  font-size: 0.875rem;
  margin: 0;
  height: auto;
}

.filter-label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  cursor: pointer;
  margin: 0;
  white-space: nowrap;

  input[type="checkbox"] {
    width: 14px;
    height: 14px;
    margin: 0;
  }
}

/* ── SYSTEMS LIST ─── */
.systems-list {
  flex: 1;
  // Sans cette ligne, la liste ne peut pas descendre sous la hauteur de son contenu : elle ne
  // scrolle pas et ce sont les cartes qui s'écrasent. Même règle que Page.vue et le module Palworld.
  min-height: 0;
  overflow-y: auto;
  padding: 1rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* ── FOOTER ─── */
.tracker-footer {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.55rem 1.5rem;
  border-top: 1px solid var(--pico-muted-border-color);
  font-size: 0.82rem;
  color: var(--pico-muted-color);

  strong { font-size: 0.9rem; }
}
</style>
