<script setup lang="ts">
import { onMounted, onBeforeUnmount } from 'vue'
import { useAdminUsersStore } from '../store/adminUsers.store'
import { fetchAdminUsers, fetchAdminRoles } from '../fetch/adminUsers.fetch'
import AdminUsersToolbar from '../components/AdminUsersToolbar.vue'
import AdminUsersHeader from '../components/AdminUsersHeader.vue'
import AdminUsersRow from '../components/AdminUsersRow.vue'
import toast from '@/services/toast'

const store = useAdminUsersStore()

const onClickOutside = () => store.closeRoleEdit()
onMounted(async () => {
  document.addEventListener('click', onClickOutside)
  store.loading = true
  try {
    const [users, roles] = await Promise.all([fetchAdminUsers(), fetchAdminRoles()])
    store.users = users
    store.roles = roles
  } catch {
    toast.error('Impossible de charger les utilisateurs')
  } finally {
    store.loading = false
  }
})
onBeforeUnmount(() => document.removeEventListener('click', onClickOutside))
</script>

<template>
  <main class="admin-users">
    <AdminUsersToolbar />
    <AdminUsersHeader />

    <template v-if="store.loading">
      <div v-for="i in 10" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.paginated.length">
      <AdminUsersRow v-for="user in store.paginated" :key="user.id" :user="user" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-account-off-outline" />
      Aucun utilisateur trouvé
    </div>
  </main>
</template>

<style scoped lang="scss">
.admin-users {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  font-size: 0.85rem;
}

.skeleton-row {
  height: 38px;
  border-radius: 0.45rem;
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
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 3rem;
  color: var(--pico-muted-color);
  font-size: 0.9rem;

  i { font-size: 1.25rem; }
}
</style>
