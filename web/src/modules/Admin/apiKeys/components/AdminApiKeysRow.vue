<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAdminApiKeysStore } from '../store/adminApiKeys.store'
import { revokeApiKey } from '../fetch/adminApiKeys.fetch'
import type { ApiKey } from '../types/adminApiKeys.types'
import { formatRelativeTime } from '@/utils/formatRelativeTime'
import { ApiException } from '@/services/ApiException'
import toast from '@/services/toast'

const props = defineProps<{ apiKey: ApiKey }>()

const store = useAdminApiKeysStore()
const visibleColumns = computed(() => store.visibleColumns)

const revoking = ref(false)
const showConfirm = ref(false)

const formatDate = (value: string) =>
  new Date(value).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString('fr-FR', { dateStyle: 'medium', timeStyle: 'short' })

const DAY_MS = 24 * 60 * 60 * 1000

// Prévenir un mois avant : le temps de générer la remplaçante et de la poser sur le NAS.
const expiresSoon = computed(() =>
  !props.apiKey.isRevoked && !props.apiKey.isExpired && new Date(props.apiKey.expiresAt).getTime() - Date.now() < 30 * DAY_MS
)

const expirationHint = computed(() =>
  props.apiKey.revokedAt
    ? `Révoquée le ${formatDateTime(props.apiKey.revokedAt)} · expiration prévue le ${formatDateTime(props.apiKey.expiresAt)}`
    : formatDateTime(props.apiKey.expiresAt)
)

const lastUsedLabel = computed(() =>
  props.apiKey.lastUsedAt ? formatRelativeTime(props.apiKey.lastUsedAt) : 'Jamais'
)

const lastUsedHint = computed(() => {
  if (!props.apiKey.lastUsedAt) return "Cette clé n'a encore jamais servi"
  const ip = props.apiKey.lastUsedIp ? ` depuis ${props.apiKey.lastUsedIp}` : ''
  return `${formatDateTime(props.apiKey.lastUsedAt)}${ip}`
})

const createdHint = computed(() =>
  `${formatDateTime(props.apiKey.createdAt)} par ${store.creatorName(props.apiKey)}`
)

const confirmRevoke = async () => {
  if (revoking.value) return
  revoking.value = true
  try {
    await revokeApiKey(props.apiKey.id)
    store.markRevokedLocally(props.apiKey.id)
    toast.success(`Clé « ${props.apiKey.name} » révoquée`)
  } catch (e) {
    toast.error(e instanceof ApiException ? e.message : 'Erreur lors de la révocation de la clé')
  } finally {
    revoking.value = false
    showConfirm.value = false
  }
}
</script>

<template>
  <div class="api-key-row" :class="{ 'api-key-row--revoked': apiKey.isRevoked }" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <template v-for="col in visibleColumns" :key="col.key">

      <!-- NOM -->
      <div v-if="col.key === 'name'" class="cell cell--name" :title="apiKey.name">
        {{ apiKey.name }}
      </div>

      <!-- DESCRIPTION -->
      <div v-else-if="col.key === 'description'" class="cell cell--muted" :title="apiKey.description">
        {{ apiKey.description }}
      </div>

      <!-- PRÉFIXE -->
      <div v-else-if="col.key === 'prefix'" class="cell cell--mono" :title="`${apiKey.keyPrefix}…`">
        {{ apiKey.keyPrefix }}…
      </div>

      <!-- SCOPES -->
      <div v-else-if="col.key === 'scopes'" class="cell cell--scopes">
        <span v-for="scope in apiKey.scopes" :key="scope" class="scope-chip" :title="scope">{{ scope }}</span>
        <span
          v-for="scope in apiKey.unknownScopes"
          :key="scope"
          class="scope-chip scope-chip--unknown"
          :title="`${scope} n'existe plus dans le catalogue : ce droit est sans effet`"
        >
          <i class="mdi mdi-alert-outline" />{{ scope }}
        </span>
      </div>

      <!-- EXPIRATION -->
      <div v-else-if="col.key === 'expiresAt'" class="cell" :title="expirationHint">
        <span v-if="apiKey.isRevoked" class="status-badge status-badge--revoked">Révoquée</span>
        <span v-else-if="apiKey.isExpired" class="status-badge status-badge--expired">Expirée</span>
        <span v-else :class="{ 'expires-soon': expiresSoon }">
          <i v-if="expiresSoon" class="mdi mdi-clock-alert-outline" />{{ formatDate(apiKey.expiresAt) }}
        </span>
      </div>

      <!-- UTILISATION -->
      <div v-else-if="col.key === 'lastUsedAt'" class="cell cell--muted" :title="lastUsedHint">
        {{ lastUsedLabel }}
      </div>

      <!-- CRÉATION -->
      <div v-else-if="col.key === 'createdAt'" class="cell cell--muted" :title="createdHint">
        {{ formatDate(apiKey.createdAt) }}
      </div>

      <!-- ACTIONS : précédées d'une piste souple pour rester à droite -->
      <template v-else-if="col.key === 'actions'">
        <span />
        <div class="cell cell--actions">
          <button v-if="!apiKey.isRevoked" class="action-btn action-btn--danger" :disabled="revoking" title="Révoquer" @click="showConfirm = true">
            <i class="mdi mdi-key-remove" />
          </button>
        </div>
      </template>

    </template>
  </div>

  <Teleport to="body">
    <div v-if="showConfirm" class="modal-overlay" @click.self="showConfirm = false">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title-group">
            <i class="mdi mdi-key-remove" aria-hidden="true"></i>
            <span class="modal-title">Révoquer cette clé</span>
          </div>
          <button class="close-btn" :disabled="revoking" @click="showConfirm = false">
            <i class="mdi mdi-close" />
          </button>
        </div>

        <p class="modal-sub">
          Cette action est définitive. <strong>{{ apiKey.name }}</strong> ({{ apiKey.keyPrefix }}…) sera
          refusée dès maintenant par l'API : le service qui l'utilise devra recevoir une nouvelle clé.
        </p>

        <div class="modal-footer">
          <button class="btn-secondary" :disabled="revoking" @click="showConfirm = false">Annuler</button>
          <button class="btn-danger" :disabled="revoking" :aria-busy="revoking" @click="confirmRevoke">
            {{ revoking ? 'Révocation…' : 'Révoquer' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.api-key-row {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.85rem;

  &:hover { box-shadow: inset 0 0 0 2px var(--pico-primary-border); }

  // Gardée lisible pour l'historique, mais visiblement hors service.
  &--revoked {
    opacity: 0.55;
    &:hover { opacity: 0.85; }
  }
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
  &--scopes { display: flex; flex-wrap: wrap; gap: 0.25rem; white-space: normal; }
}

.scope-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  max-width: 100%;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
  color: var(--pico-primary);
  font-family: monospace;
  font-size: 0.7rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;

  &--unknown {
    background: color-mix(in srgb, #ef4444 12%, transparent);
    color: #dc2626;
    text-decoration: line-through;
  }
}

.expires-soon {
  color: #d97706;
  font-weight: 600;

  i { margin-right: 0.2rem; }
}

.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;

  &--revoked {
    background: color-mix(in srgb, var(--pico-muted-color) 14%, transparent);
    color: var(--pico-muted-color);
  }

  &--expired {
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
  &:hover:not(:disabled) { color: inherit; background: var(--pico-muted-border-color); }
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
  &:hover:not(:disabled) { border-color: var(--pico-muted-color); color: inherit; }
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
