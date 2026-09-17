<script setup lang="ts">
import { onMounted } from 'vue'
import { useAdminAppLogsStore } from '../store/adminAppLogs.store'
import { fetchAdminRoles } from '../../users/fetch/adminUsers.fetch'
import AdminAppLogsToolbar from '../components/AdminAppLogsToolbar.vue'
import AdminAppLogsHeader from '../components/AdminAppLogsHeader.vue'
import AdminAppLogsRow from '../components/AdminAppLogsRow.vue'

const store = useAdminAppLogsStore()

onMounted(async () => {
  void store.load()
  try {
    store.roles = await fetchAdminRoles()
  } catch {
    // Les logs ne dépendent pas du catalogue : seul leur libellé de rôle restera indisponible.
  }
})
</script>

<template>
  <main class="admin-app-logs">
    <AdminAppLogsToolbar />

    <AdminAppLogsHeader />

    <template v-if="store.loading">
      <div v-for="i in store.pageSize" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.error">
      <div class="state error-state">
        <i class="mdi mdi-alert-circle-outline" />
        <span>{{ store.error }}</span>
        <button @click="store.load()">Réessayer</button>
      </div>
    </template>

    <template v-else-if="store.items.length">
      <AdminAppLogsRow v-for="log in store.items" :key="log.id" :log="log" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-text-box-search-outline" />
      Aucun log applicatif trouvé
    </div>
  </main>
</template>

<style scoped lang="scss">
.admin-app-logs {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.75rem;
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
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.state, .empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 3rem;
  color: var(--pico-muted-color);

  i { font-size: 1.25rem; }
  button { width: auto; margin: 0; padding: 0.3rem 0.6rem; font-size: 0.8rem; }
}

.empty { font-size: 0.9rem; }
.empty i { font-size: 1.25rem; }
.error-state { color: var(--pico-del-color); }
</style>
