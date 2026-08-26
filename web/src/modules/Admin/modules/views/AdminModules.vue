<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useAdminModulesStore } from '../store/adminModules.store'
import {
  fetchModules, fetchAllUsers, fetchRoles,
  fetchModuleUsers, addUserToModule, removeUserFromModule, updateUserModuleRole,
} from '../fetch/adminModules.fetch'
import type { AdminModule, ModuleUser } from '../types/adminModules.types'
import type { AdminRole, AdminUser } from '../../users/types/adminUsers.types'
import ModuleCreateModal from '../components/ModuleCreateModal.vue'
import ModuleEditModal from '../components/ModuleEditModal.vue'
import ModuleRolePickerModal from '../components/ModuleRolePickerModal.vue'
import toast from '@/services/toast'
import { useAuthStore } from '@/modules/Auth/auth.store'

const store = useAdminModulesStore()
const auth = useAuthStore()

//* Un admin qui touche ses propres accès module doit voir son /home à jour tout de suite,
//* sans attendre le prochain refresh de token (cf. refreshUser() dans auth.store.ts).
const refreshUserIfSelf = (userId: string) => {
  if (auth.user?.id === userId) auth.refreshUser()
}

/* ── Init ─────────────────────────────────────────────────── */
const closePopups = () => { editingRoleUserId.value = null }

onMounted(async () => {
  document.addEventListener('click', closePopups)
  document.addEventListener('scroll', closePopups, true)
  store.loadingModules = true
  try {
    const [modules, users, roles] = await Promise.all([fetchModules(), fetchAllUsers(), fetchRoles()])
    store.modules = modules
    store.allUsers = users
    store.roles = roles
  } catch {
    toast.error('Impossible de charger les modules')
  } finally {
    store.loadingModules = false
  }
})

onBeforeUnmount(() => {
  document.removeEventListener('click', closePopups)
  document.removeEventListener('scroll', closePopups, true)
})

watch(() => store.selectedModuleId, async (id) => {
  if (!id) return
  store.loadingUsers = true
  try {
    store.moduleUsers = await fetchModuleUsers(id)
  } catch {
    toast.error('Impossible de charger les membres du module')
  } finally {
    store.loadingUsers = false
  }
})

/* ── Initiales ───────────────────────────────────────────── */
const initials = (name?: string) =>
  name ? name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase() : '?'

/* ── Drag & drop ──────────────────────────────────────────── */
const dragSource = ref<'available' | 'members' | null>(null)
const dragUserId = ref<string | null>(null)
const overZone = ref<'available' | 'members' | null>(null)

const onDragStart = (source: 'available' | 'members', userId: string) => {
  dragSource.value = source
  dragUserId.value = userId
}

const onDragEnd = () => {
  dragSource.value = null
  dragUserId.value = null
  overZone.value = null
}

const onDrop = async (target: 'available' | 'members') => {
  overZone.value = null
  const uid = dragUserId.value
  const src = dragSource.value
  if (!uid || !src || src === target || !store.selectedModuleId) return

  if (target === 'members') {
    const user = store.allUsers.find(u => u.id === uid)
    if (!user) return
    pendingUser.value = user
    rolePickerOpen.value = true
  } else {
    try {
      await removeUserFromModule(store.selectedModuleId, uid)
      store.removeMemberLocally(uid)
      refreshUserIfSelf(uid)
    } catch {
      toast.error('Erreur lors du retrait de l\'utilisateur')
    }
  }

  dragSource.value = null
  dragUserId.value = null
}

/* ── Role picker ──────────────────────────────────────────── */
const rolePickerOpen = ref(false)
const pendingUser = ref<AdminUser | null>(null)

const onRolePicked = async (role: AdminRole) => {
  if (!pendingUser.value || !store.selectedModuleId) return
  const moduleId = store.selectedModuleId
  const userId = pendingUser.value.id
  try {
    await addUserToModule(moduleId, userId)
    await updateUserModuleRole(moduleId, userId, role.id)
    store.addMemberLocally(pendingUser.value, role.id, role.code)
    refreshUserIfSelf(userId)
    toast.success(`${pendingUser.value.name} ajouté au module`)
  } catch {
    toast.error('Erreur lors de l\'ajout de l\'utilisateur')
  } finally {
    rolePickerOpen.value = false
    pendingUser.value = null
  }
}

/* ── Role inline edit ─────────────────────────────────────── */
const editingRoleUserId = ref<number | null>(null)

const selectMemberRole = async (member: ModuleUser, role: AdminRole) => {
  if (!store.selectedModuleId) return
  editingRoleUserId.value = null
  try {
    await updateUserModuleRole(store.selectedModuleId, String(member.userId), role.id)
    store.updateMemberRoleLocally(member.userId, role.code)
    refreshUserIfSelf(String(member.userId))
  } catch {
    toast.error('Erreur lors de la mise à jour du rôle')
  }
}

/* ── Modales module ───────────────────────────────────────── */
const createOpen = ref(false)
const editOpen = ref(false)

const onModuleUpdated = (updated: AdminModule) => {
  store.updateModuleLocally(updated)
  editOpen.value = false
  //* Contrairement à un add/remove/rôle ciblé sur un user précis, éditer le module lui-même
  //* (actif/inactif, description…) concerne potentiellement tous ses membres dont l'admin
  //* courant — action rare, donc pas besoin de conditionner sur sa présence dans la liste.
  auth.refreshUser()
}
</script>

<template>
  <div class="admin-modules">

    <!-- ── Sidebar : liste des modules ─────────────────────── -->
    <aside class="modules-sidebar">
      <div class="sidebar-header">
        <span class="sidebar-title">Modules</span>
        <button class="btn-create" @click="createOpen = true">
          <i class="mdi mdi-plus" /> Créer
        </button>
      </div>

      <div v-if="store.loadingModules" class="sidebar-list">
        <div v-for="i in 5" :key="i" class="skeleton-module" />
      </div>
      <div v-else class="sidebar-list">
        <button
          v-for="m in store.modules"
          :key="m.id"
          class="module-item"
          :class="{ selected: store.selectedModuleId === m.id }"
          @click="store.selectModule(m.id)"
        >
          <span class="module-item-name">{{ m.name }}</span>
          <span class="active-dot" :class="m.active ? 'active-dot--on' : 'active-dot--off'" />
        </button>
      </div>
    </aside>

    <!-- ── Panneau détail ───────────────────────────────────── -->
    <section class="module-detail">

      <div v-if="!store.selectedModule" class="empty-state">
        <i class="mdi mdi-view-grid-outline" />
        Sélectionnez un module
      </div>

      <template v-else>
        <!-- Info module -->
        <div class="module-header-card">
          <div class="module-meta">
            <span class="module-name">{{ store.selectedModule.name }}</span>
            <code class="module-code">{{ store.selectedModule.code }}</code>
            <span class="active-badge" :class="store.selectedModule.active ? 'active-badge--on' : 'active-badge--off'">
              {{ store.selectedModule.active ? 'Actif' : 'Inactif' }}
            </span>
          </div>
          <p v-if="store.selectedModule.description" class="module-desc">{{ store.selectedModule.description }}</p>
          <button class="btn-edit" @click="editOpen = true">
            <i class="mdi mdi-pencil-outline" /> Modifier
          </button>
        </div>

        <!-- Drag & Drop -->
        <div class="dnd-area">

          <!-- Disponibles -->
          <div
            class="dnd-column"
            :class="{ 'dnd-column--over': overZone === 'available' }"
            @dragover.prevent="overZone = 'available'"
            @dragleave="overZone = null"
            @drop="onDrop('available')"
          >
            <div class="dnd-column-header">
              <span>Disponibles</span>
              <span class="dnd-count">{{ store.availableUsers.length }}</span>
            </div>
            <div v-if="store.loadingUsers" class="dnd-loading">
              <div v-for="i in 4" :key="i" class="skeleton-user" />
            </div>
            <div v-else class="dnd-list">
              <div
                v-for="user in store.availableUsers"
                :key="user.id"
                class="user-card"
                draggable="true"
                @dragstart="onDragStart('available', user.id)"
                @dragend="onDragEnd"
              >
                <div class="user-avatar">{{ initials(user.name) }}</div>
                <div class="user-info">
                  <span class="user-name">{{ user.name }}</span>
                  <span class="user-email">{{ user.email }}</span>
                </div>
                <i class="mdi mdi-drag drag-icon" />
              </div>
              <div v-if="!store.availableUsers.length" class="dnd-empty">Tous les utilisateurs sont membres</div>
            </div>
          </div>

          <div class="dnd-separator"><i class="mdi mdi-arrow-left-right" /></div>

          <!-- Membres -->
          <div
            class="dnd-column dnd-column--members"
            :class="{ 'dnd-column--over': overZone === 'members' }"
            @dragover.prevent="overZone = 'members'"
            @dragleave="overZone = null"
            @drop="onDrop('members')"
          >
            <div class="dnd-column-header">
              <span>Membres</span>
              <span class="dnd-count">{{ store.moduleUsers.length }}</span>
            </div>
            <div v-if="store.loadingUsers" class="dnd-loading">
              <div v-for="i in 3" :key="i" class="skeleton-user" />
            </div>
            <div v-else class="dnd-list">
              <div
                v-for="member in store.moduleUsers"
                :key="member.userId"
                class="user-card"
                draggable="true"
                @dragstart="onDragStart('members', String(member.userId))"
                @dragend="onDragEnd"
              >
                <div class="user-avatar">{{ initials(member.name) }}</div>
                <div class="user-info">
                  <span class="user-name">{{ member.name }}</span>
                  <span class="user-email">{{ member.email }}</span>
                </div>
                <div class="role-wrap" @click.stop="editingRoleUserId = editingRoleUserId === member.userId ? null : member.userId">
                  <span class="role-chip" :class="`role-chip--${member.roleCode.toLowerCase()}`">
                    {{ member.roleCode }}
                  </span>
                  <div v-if="editingRoleUserId === member.userId" class="role-popup" @click.stop>
                    <div
                      v-for="r in store.roles"
                      :key="r.id"
                      class="role-option"
                      :class="{ selected: member.roleCode === r.code }"
                      @click="selectMemberRole(member, r)"
                    >
                      {{ r.name }}
                      <i v-if="member.roleCode === r.code" class="mdi mdi-check" />
                    </div>
                  </div>
                </div>
                <i class="mdi mdi-drag drag-icon" />
              </div>
              <div v-if="!store.moduleUsers.length" class="dnd-empty">Glissez des utilisateurs ici</div>
            </div>
          </div>
        </div>
      </template>
    </section>

    <!-- ── Modales ──────────────────────────────────────────── -->
    <ModuleCreateModal
      v-if="createOpen"
      @created="m => { store.addModuleLocally(m); createOpen = false }"
      @cancel="createOpen = false"
    />

    <ModuleEditModal
      v-if="editOpen && store.selectedModule"
      :module="store.selectedModule"
      @updated="onModuleUpdated"
      @cancel="editOpen = false"
    />

    <ModuleRolePickerModal
      v-if="rolePickerOpen && pendingUser"
      :user="pendingUser"
      :roles="store.roles"
      @confirm="onRolePicked"
      @cancel="rolePickerOpen = false; pendingUser = null"
    />
  </div>
</template>

<style scoped lang="scss">
.admin-modules {
  display: grid;
  grid-template-columns: 240px 1fr;
  gap: 0.75rem;
  padding: 0.75rem;
  height: calc(100vh - 120px);
  overflow: hidden;
}

/* ── Sidebar ──────────────────────────────────────────────── */
.modules-sidebar {
  display: flex;
  flex-direction: column;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.5rem;
  overflow: hidden;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.sidebar-title {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.btn-create {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75rem;
  padding: 0.2rem 0.5rem;
  background: var(--pico-primary);
  color: var(--pico-primary-inverse);
  border: none;
  border-radius: 0.35rem;
  cursor: pointer;
  &:hover { opacity: 0.85; }
}

.sidebar-list {
  flex: 1;
  overflow-y: auto;
  padding: 0.4rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.module-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.5rem 0.65rem;
  border-radius: 0.35rem;
  border: 1px solid transparent;
  background: none;
  cursor: pointer;
  font-size: 0.85rem;
  text-align: left;
  width: 100%;

  &:hover { background: var(--pico-muted-border-color); }
  &.selected {
    background: color-mix(in srgb, var(--pico-primary) 10%, transparent);
    border-color: color-mix(in srgb, var(--pico-primary) 30%, transparent);
    color: var(--pico-primary);
  }
}

.module-item-name { flex: 1; font-weight: 500; }

.active-dot {
  width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0;
  &--on  { background: #22c55e; }
  &--off { background: var(--pico-muted-border-color); }
}

/* ── Detail ───────────────────────────────────────────────── */
.module-detail { display: flex; flex-direction: column; gap: 0.75rem; overflow: hidden; }

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  color: var(--pico-muted-color);
  font-size: 0.9rem;
  i { font-size: 2.5rem; opacity: 0.4; }
}

.module-header-card {
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-card-border-color);
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.module-meta { display: flex; align-items: center; gap: 0.65rem; }
.module-name { font-size: 1rem; font-weight: 700; }

.module-code {
  font-size: 0.75rem;
  background: var(--pico-muted-border-color);
  padding: 0.1rem 0.4rem;
  border-radius: 0.25rem;
  color: var(--pico-muted-color);
}

.active-badge {
  font-size: 0.7rem; font-weight: 600; padding: 0.1rem 0.45rem; border-radius: 999px;
  &--on  { background: color-mix(in srgb, #22c55e 12%, transparent); color: #16a34a; }
  &--off { background: color-mix(in srgb, #ef4444 12%, transparent); color: #dc2626; }
}

.module-desc { font-size: 0.8rem; color: var(--pico-muted-color); margin: 0; }

.btn-edit {
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.78rem;
  padding: 0.2rem 0.6rem;
  border: 1px solid var(--pico-muted-border-color);
  border-radius: 0.35rem;
  background: none;
  cursor: pointer;
  color: var(--pico-muted-color);
  &:hover { color: var(--pico-primary); border-color: var(--pico-primary); }
}

/* ── DnD ──────────────────────────────────────────────────── */
.dnd-area {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  gap: 0.75rem;
  overflow: hidden;
}

.dnd-separator { display: flex; align-items: center; color: var(--pico-muted-color); font-size: 1.1rem; opacity: 0.4; }

.dnd-column {
  display: flex;
  flex-direction: column;
  background: var(--pico-card-background-color);
  border: 2px dashed var(--pico-card-border-color);
  border-radius: 0.5rem;
  overflow: hidden;
  transition: border-color 0.15s, background 0.15s;

  &--over {
    border-color: var(--pico-primary);
    background: color-mix(in srgb, var(--pico-primary) 4%, var(--pico-card-background-color));
  }
  &--members { border-style: solid; }
}

.dnd-column-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--pico-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--pico-card-border-color);
}

.dnd-count { background: var(--pico-muted-border-color); padding: 0.05rem 0.4rem; border-radius: 999px; font-size: 0.7rem; }
.dnd-list { flex: 1; overflow-y: auto; padding: 0.4rem; display: flex; flex-direction: column; gap: 0.3rem; }
.dnd-empty { padding: 2rem; text-align: center; font-size: 0.8rem; color: var(--pico-muted-color); opacity: 0.6; }

/* ── User card ────────────────────────────────────────────── */
.user-card {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 0.5rem;
  border-radius: 0.4rem;
  border: 1px solid var(--pico-card-border-color);
  background: var(--pico-background-color);
  cursor: grab;
  user-select: none;
  font-size: 0.82rem;
  &:active { cursor: grabbing; }
  &:hover { border-color: var(--pico-muted-color); }
}

.user-avatar {
  width: 28px; height: 28px; border-radius: 50%;
  background: color-mix(in srgb, var(--pico-primary) 15%, transparent);
  color: var(--pico-primary);
  font-size: 0.65rem; font-weight: 700;
  display: grid; place-items: center; flex-shrink: 0;
}

.user-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 0.05rem; }
.user-name { font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.user-email { font-size: 0.72rem; color: var(--pico-muted-color); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.drag-icon { color: var(--pico-muted-color); opacity: 0.4; font-size: 1rem; flex-shrink: 0; }

/* ── Role chip + popup ────────────────────────────────────── */
.role-wrap { position: relative; cursor: pointer; }

.role-chip {
  display: inline-block;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 600;
  white-space: nowrap;
  background: color-mix(in srgb, var(--pico-muted-color) 12%, transparent);
  color: var(--pico-muted-color);
  &--admin     { background: color-mix(in srgb, var(--pico-primary) 12%, transparent); color: var(--pico-primary); }
  &--moderator { background: color-mix(in srgb, #8b5cf6 12%, transparent); color: #7c3aed; }
  &--user      { background: color-mix(in srgb, #22c55e 12%, transparent); color: #16a34a; }
  &--read_only { background: color-mix(in srgb, #f59e0b 12%, transparent); color: #d97706; }
}

.role-popup {
  position: absolute;
  top: calc(100% + 4px);
  right: 0;
  z-index: 300;
  min-width: 140px;
  background: var(--pico-card-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  overflow: hidden;
}

.role-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.4rem 0.65rem;
  font-size: 0.78rem;
  cursor: pointer;
  &:hover { background: var(--pico-muted-border-color); }
  &.selected { background: var(--pico-primary-background); color: var(--pico-primary-inverse); }
}

/* ── Skeletons ────────────────────────────────────────────── */
.skeleton-module {
  height: 36px;
  border-radius: 0.35rem;
  background: linear-gradient(90deg, var(--pico-card-background-color) 0%, var(--pico-muted-border-color) 50%, var(--pico-card-background-color) 100%);
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

.dnd-loading { display: flex; flex-direction: column; gap: 0.3rem; padding: 0.4rem; }

.skeleton-user {
  height: 48px;
  border-radius: 0.4rem;
  background: linear-gradient(90deg, var(--pico-card-background-color) 0%, var(--pico-muted-border-color) 50%, var(--pico-card-background-color) 100%);
  background-size: 200% 100%;
  animation: shimmer 1.6s ease-in-out infinite;
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
</style>
