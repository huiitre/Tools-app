<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAdminApiKeysStore } from '../store/adminApiKeys.store'
import { fetchApiKeys, fetchApiKeyScopes } from '../fetch/adminApiKeys.fetch'
import { fetchAdminUsers } from '@/modules/Admin/users/fetch/adminUsers.fetch'
import type { CreatedApiKey } from '../types/adminApiKeys.types'
import AdminApiKeysToolbar from '../components/AdminApiKeysToolbar.vue'
import AdminApiKeysHeader from '../components/AdminApiKeysHeader.vue'
import AdminApiKeysRow from '../components/AdminApiKeysRow.vue'
import ApiKeyCreateModal from '../components/ApiKeyCreateModal.vue'
import ApiKeyCreatedModal from '../components/ApiKeyCreatedModal.vue'
import toast from '@/services/toast'

const store = useAdminApiKeysStore()
const createOpen = ref(false)
const created = ref<CreatedApiKey | null>(null)

const emptyLabel = computed(() => {
  if (store.q) return 'Aucune clé ne correspond à la recherche'
  if (store.statusFilter === 'revoked') return 'Aucune clé révoquée'
  if (store.statusFilter === 'all') return "Aucune clé d'API"
  return 'Aucune clé active'
})

const load = async () => {
  store.loading = true
  try {
    const [apiKeys, scopes] = await Promise.all([fetchApiKeys(), fetchApiKeyScopes()])
    store.apiKeys = apiKeys
    store.scopes = scopes
  } catch {
    toast.error("Impossible de charger les clés d'API")
  } finally {
    store.loading = false
  }

  // Le nom des créateurs n'est qu'un confort : son absence ne bloque pas la page.
  try {
    const users = await fetchAdminUsers()
    store.userNames = Object.fromEntries(users.map(u => [u.id, u.name]))
  } catch {
    // On garde « Utilisateur #id ».
  }
}

const onCreated = (result: CreatedApiKey) => {
  createOpen.value = false
  store.addLocally(result.apiKey)
  created.value = result
}

onMounted(load)
</script>

<template>
  <main class="admin-api-keys">
    <AdminApiKeysToolbar @create="createOpen = true" />
    <AdminApiKeysHeader />

    <template v-if="store.loading">
      <div v-for="i in 10" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.paginated.length">
      <AdminApiKeysRow v-for="apiKey in store.paginated" :key="apiKey.id" :api-key="apiKey" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-key-outline" />
      {{ emptyLabel }}
    </div>

    <ApiKeyCreateModal v-if="createOpen" :scopes="store.scopes" @created="onCreated" @cancel="createOpen = false" />
    <ApiKeyCreatedModal v-if="created" :result="created" @close="created = null" />
  </main>
</template>

<style scoped lang="scss">
.admin-api-keys {
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
