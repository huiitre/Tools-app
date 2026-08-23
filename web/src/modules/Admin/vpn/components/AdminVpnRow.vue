<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAdminVpnStore } from '../store/adminVpn.store'
import { deleteVpnPeer, downloadVpnPeerConfig } from '../fetch/adminVpn.fetch'
import type { VpnPeer, VpnPeerStatus } from '../types/adminVpn.types'
import { formatRelativeTime } from '@/utils/formatRelativeTime'
import { formatBytes } from '@/utils/formatBytes'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

const props = defineProps<{ peer: VpnPeer }>()

const store = useAdminVpnStore()
const visibleColumns = computed(() => store.visibleColumns)

const deleting = ref(false)
const downloading = ref(false)
const showConfirm = ref(false)

const STATUS_LABELS: Record<VpnPeerStatus, string> = {
  connected: 'Connecté',
  idle: 'Inactif',
  never: 'Aucune connexion',
  orphan: 'Hors interface',
}

// `never` ne dit pas que le peer n'a jamais servi, seulement que l'interface n'a rien vu depuis
// son démarrage : le préciser évite de lire une panne là où il n'y en a pas.
const STATUS_HINTS: Record<VpnPeerStatus, string> = {
  connected: 'Poignée de main il y a moins de 3 minutes',
  idle: 'Tunnel connu, sans échange récent',
  never: "Aucune poignée de main depuis le démarrage de l'interface",
  orphan: "Présent dans la configuration mais absent de l'interface",
}

const statusLabel = computed(() => STATUS_LABELS[props.peer.status])
const statusHint = computed(() => STATUS_HINTS[props.peer.status])

// Le service ne renvoie qu'un compteur de secondes, jamais une date : la reconstituer ici est
// le seul moyen de réutiliser le formatage relatif du reste de l'application.
const handshakeLabel = computed(() => {
  const seconds = props.peer.handshakeSecondsAgo
  if (seconds === null) return '—'
  return formatRelativeTime(new Date(Date.now() - seconds * 1000))
})

const CHECK_LABELS: Record<string, string> = {
  keyPair: 'Paire de clés absente ou incohérente',
  serverKey: 'Clé serveur divergente dans la conf du client',
  onInterface: "Absent de l'interface",
  inConfig: 'Absent de wg0.conf',
}

const failedChecks = computed(() =>
  Object.entries(props.peer.checks)
    .filter(([, ok]) => !ok)
    .map(([key]) => CHECK_LABELS[key] ?? key)
)

const validHint = computed(() =>
  props.peer.valid ? 'Configuration exploitable' : failedChecks.value.join(' · ')
)

// La conf contient la clé privée du client : elle n'est jamais affichée, seulement téléchargée.
const download = async () => {
  if (downloading.value) return
  downloading.value = true
  try {
    const response = await downloadVpnPeerConfig(props.peer.name)
    const url = URL.createObjectURL(new Blob([response.data]))
    const a = document.createElement('a')
    a.href = url
    a.download = `${props.peer.name}.conf`
    a.click()
    URL.revokeObjectURL(url)
  } catch {
    toast.error('Erreur lors du téléchargement de la configuration')
  } finally {
    downloading.value = false
  }
}

const confirmDelete = async () => {
  if (deleting.value) return
  deleting.value = true
  try {
    await deleteVpnPeer(props.peer.name)
    store.removeLocally(props.peer.name)
    toast.success(`Peer « ${props.peer.name} » supprimé`)
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : 'Erreur lors de la suppression du peer')
  } finally {
    deleting.value = false
    showConfirm.value = false
  }
}
</script>

<template>
  <div class="vpn-row" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <template v-for="col in visibleColumns" :key="col.key">

      <!-- ÉTAT -->
      <div v-if="col.key === 'status'" class="cell" :title="statusHint">
        <span class="status-badge" :class="`status-badge--${peer.status}`">{{ statusLabel }}</span>
      </div>

      <!-- ACTIVITÉ -->
      <div v-else-if="col.key === 'handshake'" class="cell cell--muted" :title="statusHint">
        {{ handshakeLabel }}
      </div>

      <!-- REÇU -->
      <div v-else-if="col.key === 'rx'" class="cell cell--transfer">
        <i class="mdi mdi-arrow-down" />{{ formatBytes(peer.rxBytes) }}
      </div>

      <!-- ENVOYÉ -->
      <div v-else-if="col.key === 'tx'" class="cell cell--transfer">
        <i class="mdi mdi-arrow-up" />{{ formatBytes(peer.txBytes) }}
      </div>

      <!-- INTÉGRITÉ -->
      <div v-else-if="col.key === 'valid'" class="cell" :title="validHint">
        <span class="status-badge" :class="peer.valid ? 'status-badge--ok' : 'status-badge--broken'">
          {{ peer.valid ? 'OK' : 'Anomalie' }}
        </span>
      </div>

      <!-- ACTIONS : précédées d'une piste souple pour rester à droite -->
      <template v-else-if="col.key === 'actions'">
        <span />
        <div class="cell cell--actions">
          <button class="action-btn" :disabled="downloading" title="Télécharger la configuration" @click="download">
            <i class="mdi mdi-download-outline" />
          </button>
          <button class="action-btn action-btn--danger" :disabled="deleting" title="Supprimer" @click="showConfirm = true">
            <i class="mdi mdi-delete-outline" />
          </button>
        </div>
      </template>

      <!-- NOM -->
      <div v-else-if="col.key === 'name'" class="cell cell--name" :title="peer.name">
        {{ peer.name }}
      </div>

      <!-- IP / CLÉ PUBLIQUE -->
      <div v-else class="cell cell--mono" :title="col.key === 'ip' ? peer.ip : peer.publicKey">
        {{ col.key === 'ip' ? peer.ip : peer.publicKey }}
      </div>

    </template>
  </div>

  <Teleport to="body">
    <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title-group">
            <i class="mdi mdi-delete-outline" aria-hidden="true"></i>
            <span class="modal-title">Supprimer ce peer</span>
          </div>
          <button class="close-btn" :disabled="deleting" @click="showConfirm = false">
            <i class="mdi mdi-close" />
          </button>
        </div>

        <p class="modal-sub">
          Cette action est irréversible. <strong>{{ peer.name }}</strong> ({{ peer.ip }}) sera retiré
          du tunnel immédiatement et sa clé privée détruite : une reconnexion demandera une nouvelle
          configuration.
        </p>

        <div class="modal-footer">
          <button class="btn-secondary" :disabled="deleting" @click="showConfirm = false">Annuler</button>
          <button class="btn-danger" :disabled="deleting" :aria-busy="deleting" @click="confirmDelete">
            {{ deleting ? 'Suppression…' : 'Supprimer' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.vpn-row {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.85rem;

  &:hover { box-shadow: inset 0 0 0 2px var(--pico-primary-border); }
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--name { font-weight: 600; }
  &--muted { color: var(--pico-muted-color); font-size: 0.8rem; }
  &--mono { font-family: monospace; font-size: 0.78rem; color: var(--pico-muted-color); }
  &--actions { display: flex; align-items: center; justify-content: flex-end; gap: 0.15rem; overflow: visible; }
}

.cell--transfer {
  color: var(--pico-muted-color);
  font-size: 0.78rem;

  i { margin-right: 0.15rem; font-size: 0.8rem; }
}

/* ── Badges ──────────────────────────────────────────────── */
.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
  background: color-mix(in srgb, var(--pico-muted-color) 12%, transparent);
  color: var(--pico-muted-color);

  &--connected, &--ok {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    color: #16a34a;
  }
  &--idle {
    background: color-mix(in srgb, #f59e0b 12%, transparent);
    color: #d97706;
  }
  &--orphan, &--broken {
    background: color-mix(in srgb, #ef4444 12%, transparent);
    color: #dc2626;
  }
}

.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  padding: 0;
  margin: 0;
  border: 1px solid transparent;
  border-radius: var(--pico-border-radius);
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;

  i { font-size: 1rem; }

  &:hover:not(:disabled) { border-color: var(--pico-primary); color: var(--pico-primary); }
  &:disabled { opacity: 0.4; cursor: not-allowed; }

  &--danger:hover:not(:disabled) { border-color: #ef4444; color: #ef4444; }
}

/* ── Modal ── */
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.65rem;
  padding: 1.5rem;
  width: 420px;
  max-width: 90vw;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--pico-card-box-shadow);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.modal-title-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  i { font-size: 0.9rem; color: #ef4444; }
}

.modal-title { font-size: 1rem; font-weight: 700; }

.modal-sub {
  font-size: 0.85rem;
  color: var(--pico-muted-color);
  margin: 0;
  line-height: 1.5;
}

.close-btn {
  width: 1.75rem;
  height: 1.75rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  border-radius: 0.35rem;
  &:hover:not(:disabled) { color: var(--pico-color); background: var(--pico-muted-border-color); }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 0.25rem;
}

.btn-secondary {
  padding: 0.45rem 1rem;
  background: none;
  color: var(--pico-muted-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { border-color: var(--pico-color); color: var(--pico-color); }
}

.btn-danger {
  padding: 0.45rem 1rem;
  background: #ef4444;
  color: #fff;
  border: none;
  border-radius: 0.35rem;
  font-size: 0.85rem;
  cursor: pointer;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
  &:hover:not(:disabled) { background: #dc2626; }
}
</style>
