<script setup lang="ts">
import { computed } from 'vue'
import { useAdminAppLogsStore } from '../store/adminAppLogs.store'
import type { AppLogAdminItem } from '../types/adminAppLogs.types'
import UserRoleBadge from '../../users/components/UserRoleBadge.vue'

defineProps<{ log: AppLogAdminItem }>()

const store = useAdminAppLogsStore()
const visibleColumns = computed(() => store.visibleColumns)
const roleOf = (roleId: number | null) => store.roleOf(roleId)

const formatDate = (value: string | null, withTime = false) => {
  if (!value) return '—'
  try {
    return new Intl.DateTimeFormat('fr-FR', withTime
      ? { dateStyle: 'short', timeStyle: 'medium' }
      : { dateStyle: 'medium' },
    ).format(new Date(value))
  } catch {
    return value
  }
}

const status = (active: boolean | null) => active === null ? '—' : active ? 'Actif' : 'Inactif'
</script>

<template>
  <div class="log-row" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <template v-for="column in visibleColumns" :key="column.key">
    <div v-if="column.key === 'userName'" class="cell cell--name" :title="log.userName ?? undefined">{{ log.userName ?? '—' }}</div>
    <div v-else-if="column.key === 'userEmail'" class="cell" :title="log.userEmail ?? undefined">{{ log.userEmail ?? '—' }}</div>
    <div v-else-if="column.key === 'userRole'" class="cell"><UserRoleBadge :role="roleOf(log.userRoleId)" /></div>
    <div v-else-if="column.key === 'userStatus'" class="cell">
      <span v-if="log.userActive !== null" class="status-badge" :class="log.userActive ? 'status-badge--active' : 'status-badge--inactive'">
        {{ status(log.userActive) }}
      </span>
      <template v-else>—</template>
    </div>
    <div v-else-if="column.key === 'userRegisteredAt'" class="cell cell--muted" :title="formatDate(log.userRegisteredAt)">{{ formatDate(log.userRegisteredAt) }}</div>
    <div v-else-if="column.key === 'createdAt'" class="cell cell--muted" :title="formatDate(log.createdAt, true)">{{ formatDate(log.createdAt, true) }}</div>
    <div v-else-if="column.key === 'moduleName'" class="cell" :title="log.moduleName ?? undefined">{{ log.moduleName ?? '—' }}</div>
    <div v-else-if="column.key === 'areaCode'" class="cell cell--mono" :title="log.areaCode">{{ log.areaCode }}</div>
    <div v-else-if="column.key === 'actionCode'" class="cell cell--mono" :title="log.actionCode">{{ log.actionCode }}</div>
    <div v-else-if="column.key === 'ipAddress'" class="cell cell--mono" :title="log.ipAddress ?? undefined">{{ log.ipAddress ?? '—' }}</div>
    <div v-else-if="column.key === 'userAgent'" class="cell cell--mono" :title="log.userAgent ?? undefined">{{ log.userAgent ?? '—' }}</div>
    <div v-else class="cell metadata-cell">
      <i v-if="log.hasMetadata" class="mdi mdi-code-json" title="Des métadonnées sont disponibles" />
      <template v-else>—</template>
    </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
.log-row {
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
  // Le VPN porte cette hauteur via sa cellule d'actions. Les logs n'ont pas d'actions : la poser
  // sur chaque cellule donne à toute ligne exactement la même hauteur de contenu.
  min-height: 1.75rem;
  display: flex;
  align-items: center;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--name { font-weight: 600; }
  &--muted { color: var(--pico-muted-color); font-size: 0.8rem; }
  &--mono { font-family: monospace; font-size: 0.78rem; color: var(--pico-muted-color); }
}

.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
  background: color-mix(in srgb, var(--pico-muted-color) 12%, transparent);
  color: var(--pico-muted-color);

  &--active { background: color-mix(in srgb, #22c55e 12%, transparent); color: #16a34a; }
  &--inactive { background: color-mix(in srgb, #ef4444 12%, transparent); color: #dc2626; }
}

.metadata-cell {
  justify-content: center;
  text-align: center;

  i { color: var(--pico-primary); font-size: 1rem; }
}
</style>
