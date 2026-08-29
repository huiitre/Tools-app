<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { fetchGuilds, fetchBasePals, fetchPalInstanceHistory } from '../../server/fetch/palworldServerData.fetch'
import { usePalworldConfigStore } from '../../shared/palworldConfig.store'
import type {
  PalworldGuildSummary,
  PalworldPalInstanceSummary,
  PalworldPalInstanceSnapshot,
} from '../../server/types/palworldServerData.types'
import PalHistoryModal from '../components/PalHistoryModal.vue'

const configStore = usePalworldConfigStore()

const guilds = ref<PalworldGuildSummary[]>([])
const guildsLoading = ref(true)
const guildsError = ref<string | null>(null)

const guildTooltip = ref<{ visible: boolean; x: number; y: number; guild: PalworldGuildSummary | null }>({
  visible: false,
  x: 0,
  y: 0,
  guild: null,
})

function showGuildTooltip(e: MouseEvent, guild: PalworldGuildSummary) {
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  guildTooltip.value = {
    visible: true,
    x: rect.left,
    y: rect.bottom + 4,
    guild,
  }
}

function hideGuildTooltip() {
  guildTooltip.value.visible = false
}

const selectedBaseId = ref<string | null>(null)
const basePals = ref<PalworldPalInstanceSummary[]>([])
const basePalsLoading = ref(false)
const basePalsError = ref<string | null>(null)

const selectedPal = ref<PalworldPalInstanceSummary | null>(null)
const historySnapshots = ref<PalworldPalInstanceSnapshot[]>([])
const historyLoading = ref(false)
const historyError = ref<string | null>(null)

async function loadGuilds() {
  guildsLoading.value = true
  try {
    guilds.value = await fetchGuilds()
    guildsError.value = null
  } catch {
    guildsError.value = 'Impossible de charger les guildes.'
  } finally {
    guildsLoading.value = false
  }
}

async function selectBase(baseId: string) {
  selectedBaseId.value = baseId
  basePalsLoading.value = true
  try {
    basePals.value = await fetchBasePals(baseId)
    basePalsError.value = null
  } catch {
    basePalsError.value = 'Impossible de charger les Pals de cette base.'
  } finally {
    basePalsLoading.value = false
  }
}

async function openHistory(pal: PalworldPalInstanceSummary) {
  selectedPal.value = pal
  historyLoading.value = true
  try {
    historySnapshots.value = await fetchPalInstanceHistory(pal.instanceId)
    historyError.value = null
  } catch {
    historyError.value = "Impossible de charger l'historique."
  } finally {
    historyLoading.value = false
  }
}

function closeHistory() {
  selectedPal.value = null
}

function formatWorkableType(raw: string | null): string {
  if (!raw) return '—'
  return raw.replace('EPalWorkableType::', '')
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' })
}

// full_stomach est une valeur brute (pas déjà un %) : sa capacité max varie par espèce
// (pal.foodAmount). Sans cette capacité, impossible de calculer un pourcentage fiable.
function hungerPercent(fullStomach: number | null, foodAmount: number | null): number | null {
  if (fullStomach === null || !foodAmount) return null
  return Math.min(100, Math.max(0, Math.round((fullStomach / foodAmount) * 100)))
}

type SortColumn = 'pal' | 'level' | 'hunger' | 'sick' | 'work' | 'lastSeen'

const sortColumn = ref<SortColumn>('pal')
const sortDir = ref<'asc' | 'desc'>('asc')

function toggleSort(column: SortColumn) {
  if (sortColumn.value === column) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortColumn.value = column
    sortDir.value = 'asc'
  }
}

function sortValue(pal: PalworldPalInstanceSummary): number | string {
  switch (sortColumn.value) {
    case 'level': return pal.level ?? -1
    case 'hunger': return hungerPercent(pal.fullStomach, pal.palFoodAmount) ?? -1
    case 'sick': return pal.isSick ? 1 : 0
    case 'work': return formatWorkableType(pal.workableType).toLowerCase()
    case 'lastSeen': return pal.lastSeenAt
    default: return (pal.palName ?? pal.characterId).toLowerCase()
  }
}

const sortedBasePals = computed(() => {
  return [...basePals.value].sort((a, b) => {
    const va = sortValue(a)
    const vb = sortValue(b)
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return sortDir.value === 'asc' ? cmp : -cmp
  })
})

// Refresh périodique silencieux (mêmes données, pas de flash "Chargement…") —
// si un appel échoue, les données précédemment affichées restent telles quelles.
async function refreshGuilds() {
  try {
    guilds.value = await fetchGuilds()
  } catch {
    // silencieux
  }
}

async function refreshBasePals() {
  if (!selectedBaseId.value) return
  try {
    basePals.value = await fetchBasePals(selectedBaseId.value)
  } catch {
    // silencieux
  }
}

async function refreshAll() {
  await refreshGuilds()
  await refreshBasePals()
}

let refreshIntervalId: number | undefined

function restartRefreshInterval() {
  if (refreshIntervalId) clearInterval(refreshIntervalId)
  refreshIntervalId = window.setInterval(refreshAll, configStore.refreshIntervalSeconds * 1000)
}

watch(() => configStore.refreshIntervalSeconds, restartRefreshInterval)

onMounted(() => {
  configStore.hydrate()
  loadGuilds()
  restartRefreshInterval()
})

onUnmounted(() => {
  if (refreshIntervalId) clearInterval(refreshIntervalId)
})
</script>

<template>
  <div class="activity">
    <div class="activity-layout">
      <aside class="bases-sidebar">
        <div class="sidebar-header">
          <span class="sidebar-title">Bases</span>
        </div>

        <div v-if="guildsLoading" class="sidebar-list">
          <div v-for="i in 4" :key="i" class="skeleton-line" style="width: 80%; height: 1.5rem; margin: 0.5rem 1rem" />
        </div>

        <p v-else-if="guildsError" class="empty">{{ guildsError }}</p>
        <p v-else-if="!guilds.length" class="empty">Aucune guilde.</p>

        <div v-else class="sidebar-list">
          <div v-for="guild in guilds" :key="guild.guildId" class="guild-group">
            <div
              class="guild-group-name"
              @mouseenter="showGuildTooltip($event, guild)"
              @mouseleave="hideGuildTooltip"
            >
              {{ guild.name }}
            </div>
            <button
              v-for="base in guild.bases"
              :key="base.baseId"
              type="button"
              class="base-item"
              :class="{ active: selectedBaseId === base.baseId }"
              @click="selectBase(base.baseId)"
            >
              <span>Base {{ base.baseId.slice(0, 8) }}</span>
              <span class="base-pal-count">{{ base.palCount }}</span>
            </button>
          </div>
        </div>
      </aside>

      <section class="pals-panel">
        <p v-if="!selectedBaseId" class="empty-state">Sélectionne une base à gauche pour voir ses Pals.</p>

        <template v-else>
          <p v-if="basePalsLoading" class="empty-state">Chargement…</p>
          <p v-else-if="basePalsError" class="empty-state error">{{ basePalsError }}</p>
          <p v-else-if="!basePals.length" class="empty-state">Aucun Pal dans cette base.</p>

          <div v-else class="pals-table-wrap">
            <table class="pals-table">
              <thead>
                <tr>
                  <th class="sortable" @click="toggleSort('pal')">
                    Pal
                    <i v-if="sortColumn === 'pal'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                  <th class="sortable" @click="toggleSort('level')">
                    Niveau
                    <i v-if="sortColumn === 'level'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                  <th class="sortable" @click="toggleSort('hunger')">
                    Faim
                    <i v-if="sortColumn === 'hunger'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                  <th class="sortable" @click="toggleSort('sick')">
                    Malade
                    <i v-if="sortColumn === 'sick'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                  <th class="sortable" @click="toggleSort('work')">
                    Travail
                    <i v-if="sortColumn === 'work'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                  <th class="sortable" @click="toggleSort('lastSeen')">
                    Dernière activité
                    <i v-if="sortColumn === 'lastSeen'" class="mdi" :class="sortDir === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'" />
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="pal in sortedBasePals" :key="pal.instanceId" class="pal-row" @click="openHistory(pal)">
                  <td class="pal-cell">
                    <img v-if="pal.palImageUrl" :src="pal.palImageUrl" :alt="pal.palName ?? pal.characterId" width="32" height="32" loading="lazy">
                    <span>{{ pal.palName ?? pal.characterId }}</span>
                    <span v-if="pal.isAlpha" class="alpha-badge">Alpha</span>
                  </td>
                  <td>{{ pal.level ?? '—' }}</td>
                  <td>
                    <div v-if="hungerPercent(pal.fullStomach, pal.palFoodAmount) !== null" class="hunger-cell">
                      <div class="hunger-cell-fill" :style="{ width: hungerPercent(pal.fullStomach, pal.palFoodAmount) + '%' }" />
                      <span class="hunger-cell-text">{{ hungerPercent(pal.fullStomach, pal.palFoodAmount) }}%</span>
                    </div>
                    <span v-else class="muted">—</span>
                  </td>
                  <td>
                    <span v-if="pal.isSick" class="sick-badge">Oui</span>
                    <span v-else class="muted">Non</span>
                  </td>
                  <td>{{ formatWorkableType(pal.workableType) }}</td>
                  <td>{{ formatDate(pal.lastSeenAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </section>
    </div>

    <PalHistoryModal
      v-if="selectedPal"
      :loading="historyLoading"
      :error="historyError"
      :pal="selectedPal"
      :snapshots="historySnapshots"
      @close="closeHistory"
    />

    <Teleport to="body">
      <div
        v-if="guildTooltip.visible && guildTooltip.guild"
        class="guild-tooltip"
        :style="{ left: guildTooltip.x + 'px', top: guildTooltip.y + 'px' }"
      >
        <div class="guild-tooltip-title">Membres</div>
        <ul v-if="guildTooltip.guild.players.length" class="guild-tooltip-list">
          <li v-for="p in guildTooltip.guild.players" :key="p.playerUid">{{ p.name }}</li>
        </ul>
        <p v-else class="muted">Aucun membre</p>
      </div>
    </Teleport>
  </div>
</template>

<style lang="scss" scoped>
.activity {
  padding: 2rem;
  max-width: 1300px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
}

@media (max-width: 640px) {
  .activity {
    padding: 1rem;
  }
}

.activity-layout {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: 1.25rem;
  align-items: start;
}

@media (max-width: 720px) {
  .activity-layout {
    grid-template-columns: 1fr;
  }
}

/* ── Sidebar ─────────────────────────────────────────────────────── */
.bases-sidebar {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
}

.sidebar-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.sidebar-title {
  font-size: 0.875rem;
  font-weight: 600;
}

.sidebar-list {
  padding: 0.5rem 0;
  max-height: 70vh;
  overflow-y: auto;
}

.guild-group {
  padding: 0.5rem 0;

  &:not(:last-child) {
    border-bottom: 1px solid var(--pico-card-border-color);
  }
}

.guild-group-name {
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.02em;
  padding: 0.3rem 1.25rem;
  cursor: default;
  width: fit-content;
}

.guild-tooltip {
  position: fixed;
  z-index: 1000;
  pointer-events: none;
  min-width: 160px;
  background: var(--pico-card-background-color);
  border: 1px solid color-mix(in srgb, var(--pico-primary) 30%, transparent);
  box-shadow: var(--pico-card-box-shadow);
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  font-size: 0.75rem;
  color: var(--pico-color);
}

.guild-tooltip-title {
  font-weight: 600;
  color: var(--pico-primary);
  margin-bottom: 0.3rem;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.guild-tooltip-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.base-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  margin: 0;
  padding: 0.5rem 1.25rem;
  border: none;
  background: transparent;
  color: inherit;
  font-size: 0.82rem;
  cursor: pointer;
  border-radius: 0;
  text-align: left;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 6%, transparent);
  }

  &.active {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
    font-weight: 600;
  }
}

.base-pal-count {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  background: color-mix(in srgb, var(--pico-color) 8%, transparent);
  border-radius: 999px;
  padding: 0.1rem 0.5rem;
}

.base-item.active .base-pal-count {
  color: var(--pico-primary);
}

.empty {
  padding: 1rem 1.25rem;
  color: var(--pico-muted-color);
  font-size: 0.85rem;
  margin: 0;
}

/* ── Pals panel ──────────────────────────────────────────────────── */
.pals-panel {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  min-height: 200px;
}

.empty-state {
  padding: 3rem 1.5rem;
  text-align: center;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
  margin: 0;
}

.empty-state.error {
  color: #e53e3e;
}

.pals-table-wrap {
  overflow-x: auto;
}

.pals-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;

  th, td {
    text-align: left;
    padding: 0.75rem 1.25rem;
    white-space: nowrap;
  }

  th {
    color: var(--pico-muted-color);
    font-weight: 500;
    font-size: 0.78rem;
    border-bottom: 1px solid var(--pico-card-border-color);
  }

  th.sortable {
    cursor: pointer;
    user-select: none;

    &:hover { color: var(--pico-color); }

    i { font-size: 0.85rem; margin-left: 0.2rem; vertical-align: middle; }
  }

  tbody tr:not(:last-child) td {
    border-bottom: 1px solid var(--pico-card-border-color);
  }
}

.pal-row {
  cursor: pointer;

  &:hover {
    background: color-mix(in srgb, var(--pico-color) 4%, transparent);
  }
}

.pal-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;

  img {
    border-radius: 4px;
  }
}

.alpha-badge {
  font-size: 0.65rem;
  font-weight: 700;
  color: #f59e0b;
  background: color-mix(in srgb, #f59e0b 15%, transparent);
  border-radius: 999px;
  padding: 0.1rem 0.5rem;
}

.hunger-cell {
  position: relative;
  width: 90px;
  height: 16px;
  border-radius: 4px;
  background: var(--pico-muted-border-color);
  overflow: hidden;
}

.hunger-cell-fill {
  position: absolute;
  inset: 0;
  height: 100%;
  background: #f59e0b;
}

.hunger-cell-text {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 0.65rem;
  font-weight: 700;
  color: white;
  text-shadow: 0 0 2px rgba(0, 0, 0, 0.8);
}

.sick-badge {
  color: #e53e3e;
  font-weight: 600;
}

.muted {
  color: var(--pico-muted-color);
}

.skeleton-line {
  display: block;
  border-radius: 4px;
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
</style>
