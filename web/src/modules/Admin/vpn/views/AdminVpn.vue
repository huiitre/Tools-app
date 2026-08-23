<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAdminVpnStore } from '../store/adminVpn.store'
import { fetchVpnPeers } from '../fetch/adminVpn.fetch'
import AdminVpnToolbar from '../components/AdminVpnToolbar.vue'
import AdminVpnHeader from '../components/AdminVpnHeader.vue'
import AdminVpnRow from '../components/AdminVpnRow.vue'
import VpnPeerCreateModal from '../components/VpnPeerCreateModal.vue'
import toast from '@/services/toast'

const store = useAdminVpnStore()
const createOpen = ref(false)

const load = async () => {
  store.loading = true
  try {
    store.peers = await fetchVpnPeers()
  } catch {
    toast.error('Impossible de charger les peers du VPN')
  } finally {
    store.loading = false
  }
}

// Le serveur choisit le nom retenu, la clé et l'adresse : seule une relecture donne la ligne réelle.
const onCreated = async () => {
  createOpen.value = false
  await load()
}

onMounted(load)
</script>

<template>
  <main class="admin-vpn">
    <AdminVpnToolbar @create="createOpen = true" />
    <AdminVpnHeader />

    <template v-if="store.loading">
      <div v-for="i in 10" :key="i" class="skeleton-row" />
    </template>

    <template v-else-if="store.paginated.length">
      <AdminVpnRow v-for="peer in store.paginated" :key="peer.name" :peer="peer" />
    </template>

    <div v-else class="empty">
      <i class="mdi mdi-shield-off-outline" />
      Aucun peer trouvé
    </div>

    <VpnPeerCreateModal v-if="createOpen" @created="onCreated" @cancel="createOpen = false" />
  </main>
</template>

<style scoped lang="scss">
.admin-vpn {
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
