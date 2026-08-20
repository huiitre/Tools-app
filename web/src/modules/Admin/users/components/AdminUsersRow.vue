<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAdminUsersStore } from '../store/adminUsers.store'
import { updateUserRole } from '../fetch/adminUsers.fetch'
import type { AdminUser } from '../types/adminUsers.types'
import { useImagePreview } from '@/composables/useImagePreview'
import toast from '@/services/toast'

const props = defineProps<{ user: AdminUser }>()

const store = useAdminUsersStore()
const visibleColumns = computed(() => store.visibleColumns)
const { open: openPreview } = useImagePreview()

const initials = (name: string) =>
  name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()

const avatarError = ref(false)

/* ── Role popup ──────────────────────────────────────────── */
const isOpen = computed(() => store.editingRoleUserId === props.user.id)
const saving = ref(false)

const role = computed(() => store.roleOf(props.user))

const roleModifier = computed(() => role.value?.code.toLowerCase() ?? null)

const roleName = computed(() => role.value?.name ?? '—')

const openPopup = (e: MouseEvent) => {
  e.stopPropagation()
  store.openRoleEdit(props.user.id)
}

const selectRole = async (e: MouseEvent, roleId: number) => {
  e.stopPropagation()
  if (saving.value) return
  saving.value = true
  store.closeRoleEdit()
  try {
    await updateUserRole(props.user.id, roleId)
    store.updateUserRoleLocally(props.user.id, roleId)
    toast.success('Rôle mis à jour')
  } catch {
    toast.error('Erreur lors de la mise à jour du rôle')
  } finally {
    saving.value = false
  }
}

/* ── Cell values ─────────────────────────────────────────── */
const formatDate = (iso: string) => {
  try { return new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium' }).format(new Date(iso)) }
  catch { return iso }
}

const getCellValue = (key: string): string => {
  switch (key) {
    case 'name':      return props.user.name
    case 'email':     return props.user.email
    case 'active':    return props.user.active ? 'Actif' : 'Inactif'
    case 'createdAt': return formatDate(props.user.createdAt)
    default:          return ''
  }
}
</script>

<template>
  <div class="user-row" :style="{ gridTemplateColumns: store.gridTemplateColumns }">
    <!-- Avatar -->
    <div
      class="cell avatar-cell"
      :class="{ clickable: !!user.avatarUrl && !avatarError }"
      @click.stop="user.avatarUrl && !avatarError && openPreview(user.avatarUrl, user.name, 200)"
    >
      <img
        v-if="user.avatarUrl && !avatarError"
        :src="user.avatarUrl"
        :alt="user.name"
        class="avatar-img"
        @error="avatarError = true"
      />
      <div v-else class="avatar-initials">{{ initials(user.name) }}</div>
    </div>

    <template v-for="col in visibleColumns" :key="col.key">

      <!-- ROLE -->
      <div v-if="col.key === 'role'" class="cell role-cell" @click="openPopup">
        <span class="role-badge" :class="roleModifier ? `role-badge--${roleModifier}` : ''">
          {{ roleName }}
        </span>

        <div v-if="isOpen" ref="popupRef" class="role-popup" @click.stop>
          <div
            v-for="r in store.roles"
            :key="r.id"
            class="role-row"
            :class="{ selected: r.id === props.user.roleId }"
            @click="selectRole($event, r.id)"
          >
            <span class="role-row-name">{{ r.name }}</span>
            <i v-if="r.id === props.user.roleId" class="mdi mdi-check" />
          </div>
        </div>
      </div>

      <!-- STATUT -->
      <div v-else-if="col.key === 'active'" class="cell">
        <span class="status-badge" :class="user.active ? 'status-badge--active' : 'status-badge--inactive'">
          {{ getCellValue('active') }}
        </span>
      </div>

      <!-- AUTRES -->
      <div v-else class="cell" :class="{ 'cell--name': col.key === 'name' }" :title="getCellValue(col.key)">
        {{ getCellValue(col.key) }}
      </div>

    </template>
  </div>
</template>

<style scoped lang="scss">
.user-row {
  display: grid;
  align-items: center;
  column-gap: 0.6rem;
  padding: 0.45rem 0.6rem;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.45rem;
  font-size: 0.85rem;

  &:hover {
    box-shadow: inset 0 0 0 2px var(--pico-primary-border);
  }
}

.cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;

  &--name { font-weight: 600; }
}

/* ── Avatar ──────────────────────────────────────────────── */
.avatar-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: visible;

  &.clickable { cursor: pointer; }
}

.avatar-img {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  object-fit: cover;
  transition: opacity 0.15s ease;

  .clickable:hover & { opacity: 0.8; }
}

.avatar-initials {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
  font-size: 0.65rem;
  font-weight: 700;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

/* ── Role cell ───────────────────────────────────────────── */
.role-cell {
  position: relative;
  cursor: pointer;
  overflow: visible;
}

/* ── Role popup ──────────────────────────────────────────── */
.role-popup {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  z-index: 200;
  min-width: 200px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  overflow: hidden;
}

.role-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  font-size: 0.85rem;

  &:hover {
    background: var(--pico-muted-border-color);
  }

  &.selected {
    background: var(--pico-primary-background);
    color: var(--pico-primary-inverse);
  }
}

.role-row-name { flex: 1; }

/* ── Role badge ──────────────────────────────────────────── */
.role-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  background: color-mix(in srgb, var(--pico-muted-color) 12%, transparent);
  color: var(--pico-muted-color);

  &--owner, &--tech {
    background: color-mix(in srgb, #f59e0b 12%, transparent);
    color: #d97706;
  }
  &--admin {
    background: color-mix(in srgb, var(--pico-primary) 12%, transparent);
    color: var(--pico-primary);
  }
  &--moderator {
    background: color-mix(in srgb, #8b5cf6 12%, transparent);
    color: #7c3aed;
  }
  &--user {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    color: #16a34a;
  }
}

/* ── Status badge ────────────────────────────────────────── */
.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;

  &--active {
    background: color-mix(in srgb, #22c55e 12%, transparent);
    color: #16a34a;
  }
  &--inactive {
    background: color-mix(in srgb, #ef4444 12%, transparent);
    color: #dc2626;
  }
}
</style>
