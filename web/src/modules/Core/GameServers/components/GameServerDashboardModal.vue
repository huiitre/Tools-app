<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import toast from '@/services/toast'
import { executeGameServerAction, fetchGameServerDetails, fetchGameServerLive } from '../fetch/gameServers.fetch'
import GameServerActionCard from './GameServerActionCard.vue'
import type { GameServer, GameServerDetails, GameServerLive } from '../types/gameServers.types'
import { mapAdapterFor } from '../map/mapRegistry'
import GameServerMapPanel from '../map/GameServerMapPanel.vue'

const props = defineProps<{ server: GameServer }>()
const emit = defineEmits<{ close: [] }>()

const REFRESH_INTERVAL_MS = 5000
// L'appel dure quelques dizaines de millisecondes : sans plancher, l'indicateur clignerait sans
// jamais être vu. Même valeur que le dashboard Palworld.
const MIN_SPINNER_DURATION_MS = 500

// Null pour un jeu dont aucune carte n'est décrite : la section n'est alors pas rendue.
const mapAdapter = mapAdapterFor(props.server.gameCode)

const details = ref<GameServerDetails | null>(null)
const live = ref<GameServerLive | null>(null)
const groups = ref<Record<string, string[]>>({})
const loading = ref(true)
const refreshing = ref(false)
const error = ref<string | null>(null)

let refreshIntervalId: number | undefined
let previousBodyOverflow = ''

// Aucun bloc n'est masqué quand la donnée manque : le jeu ne l'expose pas, on le dit.
const UNAVAILABLE = 'Indisponible'

function metric(value: number | null | undefined, suffix = ''): string {
  return value === null || value === undefined ? UNAVAILABLE : `${value}${suffix}`
}

const playerCountLabel = computed(() => {
  if (!live.value || live.value.playerCount === null) return UNAVAILABLE
  const max = live.value.maxPlayers
  return max === null ? String(live.value.playerCount) : `${live.value.playerCount} / ${max}`
})

const fpsSubLabel = computed(() =>
  live.value?.averageFps === null || live.value?.averageFps === undefined
    ? null
    : `moyenne ${live.value.averageFps.toFixed(1)}`
)

const uptimeLabel = computed(() => {
  const seconds = live.value?.uptimeSeconds
  if (seconds === null || seconds === undefined) return UNAVAILABLE

  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (days > 0) return `${days}j ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}min`
  return `${minutes}min`
})

// Le journal arrive du plus ancien au plus récent : le plus utile est en bas de la source,
// on l'affiche en tête.
const reversedLog = computed(() => [...(live.value?.log ?? [])].reverse())

// Repliée par défaut : 118 réglages sur Palworld, personne ne veut ça déployé à l'ouverture.
const settingsCollapsed = ref(true)

// Une action à la fois : le code de celle en cours, pour ne verrouiller que sa carte.
const runningAction = ref<string | null>(null)

async function runAction(actionCode: string, label: string, parameters: Record<string, string>) {
  runningAction.value = actionCode
  try {
    await executeGameServerAction(props.server.slug, actionCode, parameters)
    toast.success(`${label} : commande envoyée`)
    // Le résultat est visible dans les données du serveur, pas dans la réponse.
    await refreshLive()
  } catch {
    toast.error(`${label} : échec`)
  } finally {
    runningAction.value = null
  }
}

const settingEntries = computed(() => Object.entries(details.value?.settings ?? {}))

function formatSettingLabel(key: string): string {
  return key
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/^b(?=[A-Z ])/, '')
    .trim()
}

function formatSettingValue(value: unknown): string {
  if (typeof value === 'boolean') return value ? 'Oui' : 'Non'
  if (Array.isArray(value)) return value.length ? value.join(', ') : '—'
  if (value === null || value === undefined || value === '') return '—'
  return String(value)
}

function healthPercent(health: number | null, maxHealth: number | null): number {
  return health !== null && maxHealth !== null && maxHealth > 0
    ? Math.round((health / maxHealth) * 100)
    : 0
}

async function refreshLive() {
  refreshing.value = true
  const startedAt = Date.now()
  try {
    // Dans le même cycle que le live : un seul aller-retour perçu, une seule gestion d'erreur.
    const [liveResult, groupsResult] = await Promise.all([
      fetchGameServerLive(props.server.slug),
      mapAdapter?.loadGroups?.() ?? Promise.resolve({}),
    ])
    live.value = liveResult
    groups.value = groupsResult
    error.value = null
  } catch {
    error.value = 'Serveur injoignable — dernières données connues affichées.'
  } finally {
    const elapsed = Date.now() - startedAt
    if (elapsed < MIN_SPINNER_DURATION_MS) {
      await new Promise(resolve => setTimeout(resolve, MIN_SPINNER_DURATION_MS - elapsed))
    }
    refreshing.value = false
  }
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('close')
}

onMounted(async () => {
  document.addEventListener('keydown', onKeydown)
  previousBodyOverflow = document.body.style.overflow
  document.body.style.overflow = 'hidden'

  try {
    // Les détails ne bougent pas : une seule fois, contrairement au reste.
    const [detailsResult, liveResult, groupsResult] = await Promise.all([
      fetchGameServerDetails(props.server.slug),
      fetchGameServerLive(props.server.slug),
      mapAdapter?.loadGroups?.() ?? Promise.resolve({}),
    ])
    details.value = detailsResult
    live.value = liveResult
    groups.value = groupsResult
  } catch {
    error.value = 'Impossible de charger les données du serveur.'
  } finally {
    loading.value = false
  }

  refreshIntervalId = window.setInterval(refreshLive, REFRESH_INTERVAL_MS)
})

onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = previousBodyOverflow
  if (refreshIntervalId) clearInterval(refreshIntervalId)
})
</script>

<template>
  <div class="dashboard-overlay" @click.self="emit('close')">
    <div class="dashboard-popup">
      <div class="dashboard-header">
        <img v-if="server.pictureUrl" class="dashboard-picture" :src="server.pictureUrl" :alt="server.gameName" />
        <span class="dashboard-title">{{ server.gameName }}</span>
        <i class="mdi mdi-close dashboard-close" @click="emit('close')" />
      </div>

      <div class="dashboard-body">
        <div v-if="error" class="error-banner">
          <i class="mdi mdi-alert-circle-outline" />
          {{ error }}
        </div>

        <div class="server-card">
          <div class="server-card-header">
            <h2 class="server-name">{{ details?.serverName ?? server.serverName }}</h2>
            <span v-if="details?.version" class="server-version">{{ details.version }}</span>
            <span v-else class="server-version muted">{{ UNAVAILABLE }}</span>
            <span class="refresh-indicator" :class="{ spinning: refreshing }" title="Actualisation automatique (5s)">
              <i class="mdi mdi-autorenew" />
            </span>
          </div>
          <p class="server-description">
            <template v-if="details?.description">{{ details.description }}</template>
            <span v-else class="muted">{{ UNAVAILABLE }}</span>
          </p>
          <p class="server-guid">
            <template v-if="details?.worldId">{{ details.worldId }}</template>
            <span v-else class="muted">{{ UNAVAILABLE }}</span>
          </p>
        </div>

        <div class="kpi-grid">
          <div class="kpi-card">
            <div class="kpi-icon kpi-icon--blue"><i class="mdi mdi-account-group-outline" /></div>
            <div class="kpi-body">
              <div class="kpi-label">Joueurs connectés</div>
              <div class="kpi-value" :class="{ unavailable: playerCountLabel === UNAVAILABLE }">
                <span v-if="loading" class="skeleton-value" />
                <template v-else>{{ playerCountLabel }}</template>
              </div>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon kpi-icon--green"><i class="mdi mdi-speedometer" /></div>
            <div class="kpi-body">
              <div class="kpi-label">FPS serveur</div>
              <div class="kpi-value" :class="{ unavailable: !loading && live?.fps === null }">
                <span v-if="loading" class="skeleton-value" />
                <template v-else>{{ metric(live?.fps) }}</template>
              </div>
              <div v-if="fpsSubLabel" class="kpi-sub">{{ fpsSubLabel }}</div>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon kpi-icon--purple"><i class="mdi mdi-calendar-outline" /></div>
            <div class="kpi-body">
              <div class="kpi-label">Jours écoulés</div>
              <div class="kpi-value" :class="{ unavailable: !loading && live?.inGameDay === null }">
                <span v-if="loading" class="skeleton-value" />
                <template v-else>{{ metric(live?.inGameDay) }}</template>
              </div>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon kpi-icon--orange"><i class="mdi mdi-home-group" /></div>
            <div class="kpi-body">
              <div class="kpi-label">Bases</div>
              <div class="kpi-value" :class="{ unavailable: !loading && live?.baseCount === null }">
                <span v-if="loading" class="skeleton-value" />
                <template v-else>{{ metric(live?.baseCount) }}</template>
              </div>
            </div>
          </div>

          <div class="kpi-card">
            <div class="kpi-icon kpi-icon--blue"><i class="mdi mdi-clock-outline" /></div>
            <div class="kpi-body">
              <div class="kpi-label">Uptime</div>
              <div class="kpi-value" :class="{ unavailable: uptimeLabel === UNAVAILABLE }">
                <span v-if="loading" class="skeleton-value" />
                <template v-else>{{ uptimeLabel }}</template>
              </div>
            </div>
          </div>
        </div>

        <div class="section">
          <div class="section-header">
            <h3 class="section-title">Joueurs connectés</h3>
          </div>

          <div v-if="loading" class="players-list">
            <div v-for="i in 3" :key="i" class="player-row">
              <span class="skeleton-line" style="width: 120px" />
              <span class="skeleton-line" style="width: 40px" />
              <span class="skeleton-line" style="width: 40px" />
              <span class="skeleton-line" style="width: 60px" />
            </div>
          </div>

          <div v-else-if="live?.players.length" class="players-table-wrap">
            <table class="players-table">
              <thead>
                <tr>
                  <th>Nom</th>
                  <th>Niveau</th>
                  <th>Compagnon</th>
                  <th>Vie</th>
                  <th>Ping</th>
                  <th>Groupe</th>
                  <th>Position</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="player in live.players" :key="player.id ?? player.name">
                  <td>{{ player.name }}</td>
                  <td>{{ player.level ?? '—' }}</td>
                  <td>
                    <span v-if="player.companion">
                      {{ player.companion.name }}
                      <template v-if="player.companion.level"> (Nv. {{ player.companion.level }})</template>
                    </span>
                    <span v-else class="muted">—</span>
                  </td>
                  <td>
                    <div v-if="player.health !== null && player.maxHealth !== null" class="hp-cell">
                      <div class="hp-cell-fill" :style="{ width: healthPercent(player.health, player.maxHealth) + '%' }" />
                      <span class="hp-cell-text">{{ player.health }} / {{ player.maxHealth }}</span>
                    </div>
                    <span v-else class="muted">—</span>
                  </td>
                  <td>{{ player.ping !== null ? `${player.ping} ms` : '—' }}</td>
                  <td>{{ player.groupName ?? '—' }}</td>
                  <td>
                    <span v-if="player.mapX !== null && player.mapY !== null">{{ player.mapX }}, {{ player.mapY }}</span>
                    <span v-else class="muted">—</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <p v-else class="empty">
            {{ live?.playerCount === null ? UNAVAILABLE : 'Aucun joueur connecté.' }}
          </p>
        </div>

        <div v-if="mapAdapter" class="section">
          <div class="section-header">
            <h3 class="section-title">Cartes</h3>
          </div>
          <div class="map-body">
            <GameServerMapPanel
              :adapter="mapAdapter"
              :players="live?.players ?? []"
              :structures="live?.structures ?? []"
              :groups="groups"
            />
          </div>
        </div>

        <div class="section">
          <div class="section-header">
            <h3 class="section-title">Journal du serveur</h3>
          </div>

          <div v-if="reversedLog.length" class="log-list">
            <p v-for="(line, index) in reversedLog" :key="index" class="log-line">{{ line }}</p>
          </div>
          <p v-else class="empty">{{ UNAVAILABLE }}</p>
        </div>

        <div v-if="details?.actions.length" class="section">
          <div class="section-header"><h3 class="section-title">Actions</h3></div>
          <div class="actions-grid">
            <GameServerActionCard
              v-for="action in details.actions"
              :key="action.code"
              :action="action"
              :players="live?.players ?? []"
              :running="runningAction === action.code"
              @submit="runAction(action.code, action.label, $event)"
            />
          </div>
        </div>

        <div class="section">
          <button
            type="button"
            class="section-header section-header--toggle"
            :aria-expanded="!settingsCollapsed"
            @click="settingsCollapsed = !settingsCollapsed"
          >
            <h3 class="section-title">Paramètres serveur</h3>
            <span class="section-count">{{ settingEntries.length }}</span>
            <i class="mdi section-chevron" :class="settingsCollapsed ? 'mdi-chevron-down' : 'mdi-chevron-up'" />
          </button>

          <template v-if="!settingsCollapsed">
            <div v-if="settingEntries.length" class="settings-grid">
              <div v-for="[key, value] in settingEntries" :key="key" class="setting-item">
                <div class="setting-label">{{ formatSettingLabel(key) }}</div>
                <div class="setting-value">{{ formatSettingValue(value) }}</div>
              </div>
            </div>
            <p v-else class="empty">{{ UNAVAILABLE }}</p>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
/* Cadre de la popup au-dessus, contenu du dashboard en dessous : les deux reprennent les styles
   de l'ancienne page serveur du module Palworld, à laquelle cet écran s'est substitué. */
.dashboard-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.dashboard-popup {
  background: var(--pico-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  width: 100%;
  max-width: 1300px;
  height: 85vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dashboard-header {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 0.85rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
  flex-shrink: 0;
}

.dashboard-picture {
  width: 3.5rem;
  height: 2rem;
  object-fit: cover;
  border-radius: calc(var(--pico-border-radius) / 2);
  flex-shrink: 0;
}

.dashboard-identity {
  display: flex;
  flex-direction: column;
  min-width: 0;
  margin-right: auto;
}

.dashboard-title {
  font-weight: 700;
  font-size: 1.05rem;
}

.dashboard-subtitle {
  font-size: 0.8rem;
  color: var(--pico-muted-color);
}

.dashboard-close {
  margin-left: auto;
  cursor: pointer;
  color: var(--pico-muted-color);
  font-size: 1.2rem;

  &:hover { color: var(--pico-color); }
}

.dashboard-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Sans ça, chaque bloc est comprimé pour tenir dans la hauteur disponible au lieu de pousser
   le défilement : la liste des joueurs s'aplatit et la carte se retrouve coupée. */
.dashboard-body > * {
  flex-shrink: 0;
}

@media (max-width: 640px) {
  .dashboard-body {
    padding: 1rem;
    gap: 1rem;
  }
}

/* ── Error ───────────────────────────────────────────────────────── */
.error-banner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: color-mix(in srgb, #e53e3e 10%, transparent);
  border: 1px solid color-mix(in srgb, #e53e3e 25%, transparent);
  color: #e53e3e;
  font-size: 0.875rem;
}

/* ── Server card ─────────────────────────────────────────────────── */
.server-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
}

.server-card-header {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.server-name {
  font-size: 1.2rem;
  font-weight: 700;
  margin: 0;
}

.server-version {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

.refresh-indicator {
  margin-left: auto;
  display: inline-flex;
  color: var(--pico-muted-color);
  opacity: 0;
  transition: opacity 0.2s ease;

  i { font-size: 1.1rem; }
}

.refresh-indicator.spinning {
  opacity: 1;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.server-description {
  margin: 0.4rem 0 0;
  font-size: 0.9rem;
  color: var(--pico-color);
}

.server-guid {
  margin: 0.5rem 0 0;
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  user-select: all;
  word-break: break-all;
}

/* ── KPI grid ────────────────────────────────────────────────────── */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.kpi-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
}

.kpi-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex-shrink: 0;

  i { font-size: 1.4rem; }

  &--blue   { background: color-mix(in srgb, #3b82f6 12%, transparent); color: #3b82f6; }
  &--green  { background: color-mix(in srgb, #22c55e 12%, transparent); color: #22c55e; }
  &--purple { background: color-mix(in srgb, #a855f7 12%, transparent); color: #a855f7; }
  &--orange { background: color-mix(in srgb, #f97316 12%, transparent); color: #f97316; }
}

.kpi-body {
  min-width: 0;
}

.kpi-label {
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  font-weight: 500;
  margin-bottom: 0.2rem;
}

.kpi-value {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1;

  /* Une donnée que le jeu n'expose pas ne doit pas s'afficher comme une valeur. */
  &.unavailable {
    font-size: 0.95rem;
    font-weight: 500;
    color: var(--pico-muted-color);
  }
}

.kpi-sub {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
  margin-top: 0.25rem;
}

/* ── Section ─────────────────────────────────────────────────────── */
.section {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 12px;
  overflow: hidden;
}

.section-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.section-title {
  font-size: 0.875rem;
  font-weight: 600;
  margin: 0;
}

/* ── Players table ───────────────────────────────────────────────── */
.players-table-wrap {
  overflow-x: auto;
}

.players-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
  margin: 0;

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

  tbody tr:not(:last-child) td {
    border-bottom: 1px solid var(--pico-card-border-color);
  }
}

.muted {
  color: var(--pico-muted-color);
}

.hp-cell {
  position: relative;
  width: 110px;
  height: 18px;
  border-radius: 4px;
  background: var(--pico-muted-border-color);
  overflow: hidden;
}

.hp-cell-fill {
  position: absolute;
  inset: 0;
  height: 100%;
  background: #22c55e;
}

.hp-cell-text {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 0.7rem;
  font-weight: 700;
  color: white;
  text-shadow: 0 0 2px rgba(0, 0, 0, 0.8), 0 0 2px rgba(0, 0, 0, 0.8);
}

/* ── Players list (squelette de chargement) ──────────────────────── */
.players-list {
  display: flex;
  flex-direction: column;
}

.player-row {
  display: flex;
  gap: 1.5rem;
  padding: 0.9rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);

  &:last-child { border-bottom: none; }
}

.skeleton-value,
.skeleton-line {
  display: inline-block;
  height: 1em;
  min-width: 60px;
  border-radius: 4px;
  background: var(--pico-muted-border-color);
  animation: skeleton-pulse 1.2s ease-in-out infinite;
}

@keyframes skeleton-pulse {
  0%, 100% { opacity: 0.45; }
  50% { opacity: 0.8; }
}

/* ── Cartes ──────────────────────────────────────────────────────── */
.map-body {
  padding: 1.25rem;
}

/* ── Actions ─────────────────────────────────────────────────────── */
.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
  padding: 1.25rem;
}

/* ── Paramètres ──────────────────────────────────────────────────── */
.section-header--toggle {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: 100%;
  margin: 0;
  background: none;
  border: none;
  border-bottom: 1px solid var(--pico-card-border-color);
  border-radius: 0;
  cursor: pointer;
  text-align: left;
  color: inherit;

  &:hover .section-title { color: var(--pico-primary); }
}

.section-count {
  font-size: 0.75rem;
  color: var(--pico-muted-color);
}

.section-chevron {
  margin-left: auto;
  color: var(--pico-muted-color);
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 0;
}

.setting-item {
  padding: 0.75rem 1.25rem;
  border-bottom: 1px solid var(--pico-card-border-color);
  border-right: 1px solid var(--pico-card-border-color);
  min-width: 0;
}

.setting-label {
  font-size: 0.72rem;
  color: var(--pico-muted-color);
  font-weight: 500;
  margin-bottom: 0.2rem;
}

.setting-value {
  font-size: 0.85rem;
  font-weight: 600;
  overflow-wrap: break-word;
}

/* ── Journal ─────────────────────────────────────────────────────── */
.log-list {
  max-height: 18rem;
  overflow: auto;
  padding: 0.75rem 1.25rem;
}

.log-line {
  margin: 0 0 0.3rem;
  font-family: var(--pico-font-family-monospace, monospace);
  font-size: 0.78rem;
  color: var(--pico-muted-color);
  white-space: pre-wrap;

  &:last-child { margin-bottom: 0; }
}

.empty {
  padding: 1.5rem 1.25rem;
  color: var(--pico-muted-color);
  font-size: 0.875rem;
  margin: 0;
}
</style>
