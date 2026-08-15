<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useAuthStore } from '@/modules/Auth/auth.store'
import { RoleCode } from '@/modules/Auth/types/auth.types'
import { usePalworldConfigStore } from '../../shared/palworldConfig.store'
import toast from '@/services/toast'
import {
  fetchServerInfo,
  fetchServerMetrics,
  fetchServerPlayers,
  fetchServerSettings,
  fetchServerGameData,
  announceServerMessage,
  kickServerPlayer,
  saveServerWorld,
  banServerPlayer,
  unbanServerPlayer,
  shutdownServer,
  stopServer,
} from '../fetch/palworldServer.fetch'
import { fetchGuilds } from '../fetch/palworldServerData.fetch'
import type {
  PalworldBase,
  PalworldBasePal,
  PalworldGameData,
  PalworldGamePlayer,
  PalworldServerInfo,
  PalworldServerMetrics,
  PalworldServerPlayer,
  PalworldServerSettings,
} from '../types/palworldServer.types'
import type { PalworldGuildSummary } from '../types/palworldServerData.types'
import PalworldPlayerDetailsModal from '../components/PalworldPlayerDetailsModal.vue'
import PalworldOverviewMap from '../components/PalworldOverviewMap.vue'

const authStore = useAuthStore()
const canModerate = computed(() => authStore.hasModuleAccess('PALWORLD', RoleCode.MODERATOR))
const canAdminister = computed(() => authStore.hasModuleAccess('PALWORLD', RoleCode.ADMIN))

const configStore = usePalworldConfigStore()

const info = ref<PalworldServerInfo | null>(null)
const metrics = ref<PalworldServerMetrics | null>(null)
// Le endpoint léger /players fournit le ping (absent de /game-data) : on fusionne les deux par userId.
const lightPlayers = ref<PalworldServerPlayer[]>([])
const gameData = ref<PalworldGameData | null>(null)
const settings = ref<PalworldServerSettings | null>(null)
const error = ref<string | null>(null)
const loading = ref(true)
const refreshing = ref(false)

const guilds = ref<PalworldGuildSummary[]>([])

interface PlayerRow extends PalworldGamePlayer {
  ping: number | null
}

const playerRows = computed<PlayerRow[]>(() =>
  (gameData.value?.players ?? []).map(player => ({
    ...player,
    ping: lightPlayers.value.find(lp => lp.userId === player.userId)?.ping ?? null,
  })),
)

const bases = computed<PalworldBase[]>(() => {
  const liveBases = gameData.value?.bases ?? []
  const persistedBases: PalworldBase[] = guilds.value.flatMap(guild => guild.bases
    .filter(base => base.positionX !== null && base.positionY !== null)
    .map(base => ({
      name: `Base ${base.baseId.slice(0, 8)}`,
      guildId: guild.guildId,
      guildName: guild.name,
      locationX: base.positionX as number,
      locationY: base.positionY as number,
      locationZ: base.positionZ ?? 0,
      mapX: Math.round(base.positionX as number),
      mapY: Math.round(base.positionY as number),
      palCount: base.palCount,
    })))

  // Le snapshot couvre aussi les guildes hors ligne. Une base live remplace
  // celle du snapshot lorsqu'elle est à la même position.
  const merged = [...persistedBases]
  for (const liveBase of liveBases) {
    const duplicateIndex = merged.findIndex(base =>
      base.guildId === liveBase.guildId
      && Math.abs(base.locationX - liveBase.locationX) < 1
      && Math.abs(base.locationY - liveBase.locationY) < 1)
    // Le nombre de Pals n'existe que dans le snapshot : sans ce report, il disparaîtrait
    // dès qu'un membre de la guilde se connecte et que la base remonte en direct.
    const replaced = duplicateIndex >= 0 ? merged.splice(duplicateIndex, 1)[0] : undefined
    merged.push({ ...liveBase, palCount: liveBase.palCount ?? replaced?.palCount ?? null })
  }
  return merged
})

const GUILD_COLOR_PALETTE = [
  '#3b82f6', '#f97316', '#22c55e', '#a855f7', '#ef4444',
  '#06b6d4', '#eab308', '#ec4899', '#84cc16', '#6366f1',
]

const guildColors = computed<Record<string, string>>(() => {
  const ids = Array.from(new Set([...playerRows.value.map(p => p.guildId), ...bases.value.map(b => b.guildId)])).sort()
  const map: Record<string, string> = {}
  ids.forEach((id, index) => {
    map[id] = GUILD_COLOR_PALETTE[index % GUILD_COLOR_PALETTE.length]
  })
  return map
})

function hpPercent(hp: number, maxHp: number): number {
  return maxHp > 0 ? Math.round((hp / maxHp) * 100) : 0
}

const MIN_SPINNER_DURATION_MS = 500
let refreshIntervalId: number | undefined

// ── Détails joueur (popup) ──────────────────────────────────────
const showPlayerDetails = ref(false)
const loadingPlayerDetails = ref(false)
const playerDetailsError = ref<string | null>(null)
const selectedPlayerUserId = ref<string | null>(null)
const selectedGamePlayer = ref<PalworldGamePlayer | null>(null)
const selectedPlayerBases = ref<PalworldBase[]>([])
const selectedPlayerBasePals = ref<PalworldBasePal[]>([])

async function loadPlayerDetails() {
  if (!selectedPlayerUserId.value) return
  try {
    const gameData = await fetchServerGameData()
    const gamePlayer = gameData.players.find(p => p.userId === selectedPlayerUserId.value)
    if (!gamePlayer) {
      playerDetailsError.value = 'Détails indisponibles pour ce joueur.'
      return
    }
    selectedGamePlayer.value = gamePlayer
    selectedPlayerBases.value = gameData.bases.filter(b => b.guildId === gamePlayer.guildId)
    selectedPlayerBasePals.value = gameData.basePals.filter(p => p.guildId === gamePlayer.guildId)
    playerDetailsError.value = null
  } catch {
    playerDetailsError.value = 'Impossible de récupérer les détails du joueur.'
  }
}

async function openPlayerDetails(player: PlayerRow) {
  selectedPlayerUserId.value = player.userId
  showPlayerDetails.value = true
  loadingPlayerDetails.value = true
  playerDetailsError.value = null
  selectedGamePlayer.value = null
  selectedPlayerBases.value = []
  selectedPlayerBasePals.value = []

  await loadPlayerDetails()
  loadingPlayerDetails.value = false
}

function closePlayerDetails() {
  showPlayerDetails.value = false
  selectedPlayerUserId.value = null
}

const formatUptime = (seconds: number) => {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return `${hours}h ${minutes}min`
}

const settingEntries = computed(() => Object.entries(settings.value ?? {}))

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

async function refreshAll() {
  refreshing.value = true
  const startedAt = Date.now()
  try {
    const [infoResult, metricsResult, lightPlayersResult, gameDataResult] = await Promise.all([
      fetchServerInfo(),
      fetchServerMetrics(),
      fetchServerPlayers(),
      fetchServerGameData(),
    ])
    info.value = infoResult
    metrics.value = metricsResult
    lightPlayers.value = lightPlayersResult
    gameData.value = gameDataResult
    error.value = null

    if (showPlayerDetails.value) {
      await loadPlayerDetails()
    }
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

function restartRefreshInterval() {
  if (refreshIntervalId) clearInterval(refreshIntervalId)
  refreshIntervalId = window.setInterval(refreshAll, configStore.refreshIntervalSeconds * 1000)
}

watch(() => configStore.refreshIntervalSeconds, restartRefreshInterval)

// Les guildes alimentent les bases persistées de la carte et les noms de joueurs des
// infobulles : un échec vide la carte de ses bases hors ligne, il ne doit pas passer inaperçu.
async function loadGuilds() {
  try {
    guilds.value = await fetchGuilds()
  } catch {
    toast.error('Impossible de charger les guildes.')
  }
}

async function refreshPlayers() {
  try {
    const [lightPlayersResult, gameDataResult] = await Promise.all([fetchServerPlayers(), fetchServerGameData()])
    lightPlayers.value = lightPlayersResult
    gameData.value = gameDataResult
  } catch {
    // silencieux — la liste reste affichée telle quelle
  }
}

onMounted(async () => {
  configStore.hydrate()

  try {
    const [infoResult, metricsResult, lightPlayersResult, gameDataResult, settingsResult] = await Promise.all([
      fetchServerInfo(),
      fetchServerMetrics(),
      fetchServerPlayers(),
      fetchServerGameData(),
      fetchServerSettings(),
    ])
    info.value = infoResult
    metrics.value = metricsResult
    lightPlayers.value = lightPlayersResult
    gameData.value = gameDataResult
    settings.value = settingsResult
  } catch {
    error.value = 'Impossible de charger les données du serveur.'
  } finally {
    loading.value = false
  }

  loadGuilds()
  restartRefreshInterval()
})

onUnmounted(() => {
  if (refreshIntervalId) clearInterval(refreshIntervalId)
})

// ── Annonce ──────────────────────────────────────────────────────
const announceMessage = ref('')
const announcing = ref(false)

async function handleAnnounce() {
  if (!announceMessage.value.trim()) return
  announcing.value = true
  try {
    await announceServerMessage(announceMessage.value.trim())
    toast.success('Message diffusé')
    announceMessage.value = ''
  } catch {
    toast.error('Échec de la diffusion du message')
  } finally {
    announcing.value = false
  }
}

// ── Sauvegarde ───────────────────────────────────────────────────
const saving = ref(false)

async function handleSave() {
  saving.value = true
  try {
    await saveServerWorld()
    toast.success('Monde sauvegardé')
  } catch {
    toast.error('Échec de la sauvegarde')
  } finally {
    saving.value = false
  }
}

// ── Expulsion ────────────────────────────────────────────────────
const kickUserId = ref('')
const kickMessage = ref('')
const kicking = ref(false)

async function handleKick() {
  if (!kickUserId.value) return
  if (!confirm('Expulser ce joueur du serveur ?')) return
  kicking.value = true
  try {
    await kickServerPlayer(kickUserId.value, kickMessage.value.trim())
    toast.success('Joueur expulsé')
    kickUserId.value = ''
    kickMessage.value = ''
    await refreshPlayers()
  } catch {
    toast.error("Échec de l'expulsion")
  } finally {
    kicking.value = false
  }
}

// ── Bannissement ─────────────────────────────────────────────────
const banUserId = ref('')
const banMessage = ref('')
const banning = ref(false)

async function handleBan() {
  if (!banUserId.value.trim()) return
  if (!confirm('Bannir ce joueur du serveur ?')) return
  banning.value = true
  try {
    await banServerPlayer(banUserId.value.trim(), banMessage.value.trim())
    toast.success('Joueur banni')
    banUserId.value = ''
    banMessage.value = ''
    await refreshPlayers()
  } catch {
    toast.error('Échec du bannissement')
  } finally {
    banning.value = false
  }
}

// ── Débannissement ───────────────────────────────────────────────
const unbanUserId = ref('')
const unbanning = ref(false)

async function handleUnban() {
  if (!unbanUserId.value.trim()) return
  unbanning.value = true
  try {
    await unbanServerPlayer(unbanUserId.value.trim())
    toast.success('Joueur débanni')
    unbanUserId.value = ''
  } catch {
    toast.error('Échec du débannissement')
  } finally {
    unbanning.value = false
  }
}

// ── Arrêt programmé ──────────────────────────────────────────────
const shutdownWaittime = ref(60)
const shutdownMessage = ref('')
const shuttingDown = ref(false)

async function handleShutdown() {
  if (!confirm(`Arrêter le serveur dans ${shutdownWaittime.value}s ?`)) return
  shuttingDown.value = true
  try {
    await shutdownServer(shutdownWaittime.value, shutdownMessage.value.trim())
    toast.success('Arrêt du serveur programmé')
  } catch {
    toast.error("Échec de la programmation de l'arrêt")
  } finally {
    shuttingDown.value = false
  }
}

// ── Arrêt immédiat ───────────────────────────────────────────────
const stopping = ref(false)

async function handleStop() {
  if (!confirm('Arrêter le serveur immédiatement ? Cette action est irréversible.')) return
  stopping.value = true
  try {
    await stopServer()
    toast.success('Serveur arrêté')
  } catch {
    toast.error("Échec de l'arrêt du serveur")
  } finally {
    stopping.value = false
  }
}
</script>

<template>
  <div class="dashboard">
    <div v-if="error" class="error-banner">
      <i class="mdi mdi-alert-circle-outline" />
      {{ error }}
    </div>

    <!-- Info serveur -->
    <div class="server-card">
      <div class="server-card-header">
        <h2 class="server-name">
          <template v-if="loading"><span class="skeleton-value" style="width: 220px" /></template>
          <template v-else>{{ info?.servername ?? '—' }}</template>
        </h2>
        <span v-if="!loading && info" class="server-version">v{{ info.version }}</span>
        <span class="refresh-indicator" :class="{ spinning: refreshing }" title="Actualisation automatique (5s)">
          <i class="mdi mdi-autorenew" />
        </span>
      </div>
      <p v-if="!loading && info" class="server-description">{{ info.description }}</p>
      <p v-if="!loading && info" class="server-guid">{{ info.worldguid }}</p>
    </div>

    <!-- KPI cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--blue">
          <i class="mdi mdi-account-group-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Joueurs connectés</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ metrics?.currentPlayerNum ?? '—' }} / {{ metrics?.maxPlayerNum ?? '—' }}</template>
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--green">
          <i class="mdi mdi-speedometer" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">FPS serveur</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ metrics?.serverFps ?? '—' }}</template>
          </div>
          <div v-if="!loading && metrics" class="kpi-sub">
            moyenne {{ metrics.serverFpsAverage.toFixed(1) }}
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--purple">
          <i class="mdi mdi-calendar-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Jours écoulés</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ metrics?.days ?? '—' }}</template>
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--orange">
          <i class="mdi mdi-home-group" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Bases</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ metrics?.baseCampNum ?? '—' }}</template>
          </div>
        </div>
      </div>

      <div class="kpi-card">
        <div class="kpi-icon kpi-icon--blue">
          <i class="mdi mdi-clock-outline" />
        </div>
        <div class="kpi-body">
          <div class="kpi-label">Uptime</div>
          <div class="kpi-value">
            <template v-if="loading"><span class="skeleton-value" /></template>
            <template v-else>{{ formatUptime(metrics?.uptime ?? 0) }}</template>
          </div>
        </div>
      </div>
    </div>

    <!-- Cartes -->
    <div class="section">
      <div class="section-header">
        <h3 class="section-title">Cartes</h3>
      </div>

      <div v-if="!loading" class="maps-grid">
        <PalworldOverviewMap :players="playerRows" :bases="bases" :guilds="guilds" :guild-colors="guildColors" />
      </div>
    </div>

    <!-- Joueurs -->
    <div class="section">
      <div class="section-header">
        <h3 class="section-title">Joueurs connectés</h3>
      </div>

      <div v-if="loading" class="players-list">
        <div v-for="i in 3" :key="i" class="player-row skeleton-row">
          <span class="skeleton-line" style="width: 120px" />
          <span class="skeleton-line" style="width: 40px" />
          <span class="skeleton-line" style="width: 40px" />
          <span class="skeleton-line" style="width: 60px" />
        </div>
      </div>

      <div v-else-if="playerRows.length" class="players-table-wrap">
        <table class="players-table">
          <thead>
            <tr>
              <th>Nom</th>
              <th>Niveau</th>
              <th>Pal actif</th>
              <th>Vie</th>
              <th>Ping</th>
              <th>Position</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="player in playerRows" :key="player.userId">
              <td><span class="player-name" @click="openPlayerDetails(player)">{{ player.name }}</span></td>
              <td>{{ player.level }}</td>
              <td>
                <span v-if="player.activePal">{{ player.activePal.name }} (Nv. {{ player.activePal.level }})</span>
                <span v-else class="muted">—</span>
              </td>
              <td>
                <div class="hp-cell">
                  <div class="hp-cell-fill" :style="{ width: hpPercent(player.hp, player.maxHp) + '%' }" />
                  <span class="hp-cell-text">{{ player.hp }} / {{ player.maxHp }}</span>
                </div>
              </td>
              <td>{{ player.ping !== null ? `${Math.round(player.ping)} ms` : '—' }}</td>
              <td>{{ player.mapX }}, {{ player.mapY }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <p v-else-if="!loading" class="empty">Aucun joueur connecté.</p>
    </div>


    <!-- Paramètres -->
    <div class="section">
      <div class="section-header">
        <h3 class="section-title">Paramètres serveur</h3>
      </div>

      <div v-if="loading" class="settings-grid">
        <div v-for="i in 8" :key="i" class="setting-item">
          <span class="skeleton-line" style="width: 100px" />
          <span class="skeleton-line" style="width: 60px; margin-top: 0.4rem" />
        </div>
      </div>

      <div v-else-if="settingEntries.length" class="settings-grid">
        <div v-for="[key, value] in settingEntries" :key="key" class="setting-item">
          <div class="setting-label">{{ formatSettingLabel(key) }}</div>
          <div class="setting-value">{{ formatSettingValue(value) }}</div>
        </div>
      </div>

      <p v-else class="empty">Paramètres indisponibles.</p>
    </div>

    <!-- Actions -->
    <div v-if="canModerate" class="section">
      <div class="section-header">
        <h3 class="section-title">Actions</h3>
      </div>

      <div class="actions-grid">
        <form class="action-card" @submit.prevent="handleAnnounce">
          <div class="action-label">
            <i class="mdi mdi-bullhorn-outline" />
            Annonce
          </div>
          <input v-model="announceMessage" type="text" placeholder="Message à diffuser" :disabled="announcing">
          <button type="submit" :disabled="announcing || !announceMessage.trim()" :aria-busy="announcing">
            Diffuser
          </button>
        </form>

        <div class="action-card">
          <div class="action-label">
            <i class="mdi mdi-content-save-outline" />
            Sauvegarde
          </div>
          <p class="action-hint">Sauvegarde immédiate du monde.</p>
          <button type="button" :disabled="saving" :aria-busy="saving" @click="handleSave">
            Sauvegarder
          </button>
        </div>

        <form class="action-card" @submit.prevent="handleKick">
          <div class="action-label">
            <i class="mdi mdi-account-remove-outline" />
            Expulsion
          </div>
          <select v-model="kickUserId" :disabled="kicking || !playerRows.length">
            <option value="" disabled>{{ playerRows.length ? 'Choisir un joueur' : 'Aucun joueur connecté' }}</option>
            <option v-for="p in playerRows" :key="p.userId" :value="p.userId">{{ p.name }}</option>
          </select>
          <input v-model="kickMessage" type="text" placeholder="Raison (optionnel)" :disabled="kicking">
          <button type="submit" :disabled="kicking || !kickUserId" :aria-busy="kicking">
            Expulser
          </button>
        </form>

        <template v-if="canAdminister">
          <form class="action-card action-card--danger" @submit.prevent="handleBan">
            <div class="action-label">
              <i class="mdi mdi-account-cancel-outline" />
              Bannissement
            </div>
            <input v-model="banUserId" type="text" placeholder="ID du joueur (steam_...)" :disabled="banning">
            <input v-model="banMessage" type="text" placeholder="Raison (optionnel)" :disabled="banning">
            <button type="submit" class="danger" :disabled="banning || !banUserId.trim()" :aria-busy="banning">
              Bannir
            </button>
          </form>

          <form class="action-card" @submit.prevent="handleUnban">
            <div class="action-label">
              <i class="mdi mdi-account-check-outline" />
              Débannissement
            </div>
            <input v-model="unbanUserId" type="text" placeholder="ID du joueur (steam_...)" :disabled="unbanning">
            <button type="submit" :disabled="unbanning || !unbanUserId.trim()" :aria-busy="unbanning">
              Débannir
            </button>
          </form>

          <form class="action-card action-card--danger" @submit.prevent="handleShutdown">
            <div class="action-label">
              <i class="mdi mdi-timer-sand" />
              Arrêt programmé
            </div>
            <input v-model.number="shutdownWaittime" type="number" min="0" placeholder="Délai (secondes)" :disabled="shuttingDown">
            <input v-model="shutdownMessage" type="text" placeholder="Message (optionnel)" :disabled="shuttingDown">
            <button type="submit" class="danger" :disabled="shuttingDown" :aria-busy="shuttingDown">
              Programmer l'arrêt
            </button>
          </form>

          <div class="action-card action-card--danger">
            <div class="action-label">
              <i class="mdi mdi-stop-circle-outline" />
              Arrêt immédiat
            </div>
            <p class="action-hint">Coupe le serveur sans délai.</p>
            <button type="button" class="danger" :disabled="stopping" :aria-busy="stopping" @click="handleStop">
              Arrêter maintenant
            </button>
          </div>
        </template>
      </div>
    </div>

    <PalworldPlayerDetailsModal
      v-if="showPlayerDetails"
      :loading="loadingPlayerDetails"
      :error="playerDetailsError"
      :player="selectedGamePlayer"
      :bases="selectedPlayerBases"
      :base-pals="selectedPlayerBasePals"
      @close="closePlayerDetails"
    />
  </div>
</template>

<style lang="scss" scoped>
.dashboard {
  padding: 2rem;
  max-width: 1300px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

@media (max-width: 640px) {
  .dashboard {
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

/* ── Maps ────────────────────────────────────────────────────────── */
.maps-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.25rem;
  padding: 1.25rem;
}

@media (max-width: 720px) {
  .maps-grid {
    grid-template-columns: 1fr;
  }
}

/* ── Players table ───────────────────────────────────────────────── */
.players-table-wrap {
  overflow-x: auto;
}

.players-table {
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

  tbody tr:not(:last-child) td {
    border-bottom: 1px solid var(--pico-card-border-color);
  }
}

.player-name {
  cursor: pointer;
  font-weight: 500;

  &:hover {
    color: var(--pico-primary);
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

/* ── Players list (skeleton) ────────────────────────────────────── */
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

/* ── Settings ────────────────────────────────────────────────────── */
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

/* ── Actions ─────────────────────────────────────────────────────── */
.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
  padding: 1.25rem;
}

.action-card {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem;
  margin: 0;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 10px;

  input, select {
    margin: 0;
    padding: 0.4rem 0.6rem;
    font-size: 0.85rem;
    height: auto;
  }

  button {
    margin: 0.25rem 0 0;
    padding: 0.4rem 0.8rem;
    font-size: 0.85rem;
    width: auto;
    align-self: flex-start;
  }
}

.action-card--danger {
  border-color: color-mix(in srgb, #e53e3e 30%, var(--pico-card-border-color));
}

.action-label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.85rem;
  font-weight: 600;

  i { font-size: 1rem; color: var(--pico-muted-color); }
}

.action-hint {
  margin: 0;
  font-size: 0.78rem;
  color: var(--pico-muted-color);
}

button.danger {
  background: #e53e3e;
  border-color: #e53e3e;
  color: white;

  &:hover:not(:disabled) {
    background: color-mix(in srgb, #e53e3e 85%, black);
  }
}

/* ── Skeleton ────────────────────────────────────────────────────── */
@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

%shimmer {
  background: linear-gradient(
    90deg,
    var(--pico-card-background-color) 0%,
    var(--pico-muted-border-color) 50%,
    var(--pico-card-background-color) 100%
  );
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
  border-radius: 4px;
}

.skeleton-value {
  @extend %shimmer;
  display: block;
  width: 60px;
  height: 1.75rem;
}

.skeleton-row {
  pointer-events: none;
}

.skeleton-line {
  @extend %shimmer;
  display: block;
  height: 0.875rem;
}

/* ── Empty ───────────────────────────────────────────────────────── */
.empty {
  padding: 1.5rem 1.25rem;
  color: var(--pico-muted-color);
  font-size: 0.875rem;
  margin: 0;
}
</style>
