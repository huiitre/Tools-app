<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { useBankManagementStore } from '../bankmanagement.store';
import { useHdvSnifferStore } from '@/modules/Dofus/hdv/hdvSniffer.store';
import { useDofusStore } from '@/modules/Dofus/dofus.store';
import { useOS } from '@/composables/useOS';
import { useEnv } from '@/composables/useEnv';
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices';
import { formatNumber } from '@/utils/formatNumber';
import BankItemCard from '../components/BankItemCard.vue';
import BankTypeFilterButton from '../components/BankTypeFilterButton.vue';

const store = useBankManagementStore();
const hdvStore = useHdvSnifferStore();
const dofusStore = useDofusStore();
const { isLinux } = useOS();
const { isElectron } = useEnv();
const { get: getPrice } = useItemPrices();

/* ========================= STATE ========================= */
const searchQuery = ref('');
const sortBy = ref<'name' | 'type' | 'level' | 'quantity' | 'price'>('name');
const sortOrder = ref<'asc' | 'desc'>('asc');
const selectedTypeIds = ref<number[]>([]);
const now = ref(Date.now());
let tickInterval: any = null;

/* ========================= STATS ========================= */
const bankStats = computed(() => {
  let totalValue = 0;
  let withPrice = 0;
  let withoutPrice = 0;

  for (const item of store.items) {
    if (!item.metadata) {
      withoutPrice++;
      continue;
    }

    const priceData = getPrice(item.metadata.id);
    if (priceData && priceData.communityAveragePrice > 0) {
      totalValue += priceData.communityAveragePrice * item.quantity;
      withPrice++;
    } else {
      withoutPrice++;
    }
  }

  return { totalValue, withPrice, withoutPrice };
});

const totalValueDisplay = computed(() => {
  const val = bankStats.value.totalValue;
  if (val >= 1000000000) return (val / 1000000000).toFixed(2) + 'b';
  if (val >= 1000000) return (val / 1000000).toFixed(1) + 'm';
  return formatNumber(val);
});

/* ========================= DATE RELATIVE ========================= */
const lastSyncDisplay = computed(() => {
  if (!store.lastSync) return 'Jamais';
  
  const diffInSeconds = Math.floor((now.value - store.lastSync) / 1000);
  
  if (diffInSeconds < 60) return 'à l\'instant';
  
  const diffInMinutes = Math.floor(diffInSeconds / 60);
  if (diffInMinutes < 60) return `il y a ${diffInMinutes} min`;
  
  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) return `il y a ${diffInHours} h`;
  
  return 'il y a plus d\'un jour';
});

/* ========================= CONFIG / PERSISTENCE ========================= */
const loadConfig = () => {
  const savedSort = localStorage.getItem('bank.sort_config');
  if (savedSort) {
    const config = JSON.parse(savedSort);
    sortBy.value = config.sortBy || 'name';
    sortOrder.value = config.sortOrder || 'asc';
  }

  const savedFilters = localStorage.getItem('bank.filters_config');
  if (savedFilters) {
    const config = JSON.parse(savedFilters);
    selectedTypeIds.value = config.selectedTypeIds || [];
  }
};

const saveSortConfig = () => {
  localStorage.setItem('bank.sort_config', JSON.stringify({
    sortBy: sortBy.value,
    sortOrder: sortOrder.value
  }));
};

const saveFiltersConfig = () => {
  localStorage.setItem('bank.filters_config', JSON.stringify({
    selectedTypeIds: selectedTypeIds.value
  }));
};

/* ========================= LOGIQUE FILTRE & TRI ========================= */
const filteredAndSortedItems = computed(() => {
  let result = [...store.items];

  if (selectedTypeIds.value.length > 0) {
    result = result.filter(item => 
      item.metadata && selectedTypeIds.value.includes(item.metadata.itemType.id)
    );
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase();
    result = result.filter(item => 
      item.metadata?.name.toLowerCase().includes(q) || 
      item.assetId.toString().includes(q)
    );
  }

  result.sort((a, b) => {
    let valA: any = 0;
    let valB: any = 0;

    switch (sortBy.value) {
      case 'name':
        valA = a.metadata?.name || '';
        valB = b.metadata?.name || '';
        break;
      case 'type':
        valA = a.metadata?.itemType.name || '';
        valB = b.metadata?.itemType.name || '';
        break;
      case 'level':
        valA = a.metadata?.level || 0;
        valB = b.metadata?.level || 0;
        break;
      case 'quantity':
        valA = a.quantity;
        valB = b.quantity;
        break;
      case 'price':
        const priceA = a.metadata ? getPrice(a.metadata.id)?.communityAveragePrice || 0 : 0;
        const priceB = b.metadata ? getPrice(b.metadata.id)?.communityAveragePrice || 0 : 0;
        valA = priceA * a.quantity;
        valB = priceB * b.quantity;
        break;
    }

    if (valA < valB) return sortOrder.value === 'asc' ? -1 : 1;
    if (valA > valB) return sortOrder.value === 'asc' ? 1 : -1;
    return 0;
  });

  return result;
});

const toggleSortOrder = () => {
  sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc';
  saveSortConfig();
};

/* ========================= LIFECYCLE ========================= */
onMounted(async () => {
  loadConfig();
  if (isEnvCompatible.value) {
    hdvStore.checkSystem();
  }
  await store.init();

  // "Tick" lent (30 secondes) pour rafraîchir les dates relatives
  tickInterval = setInterval(() => {
    now.value = Date.now();
  }, 30000);
});

onBeforeUnmount(() => {
  if (tickInterval) clearInterval(tickInterval);
});

watch([sortBy, sortOrder], saveSortConfig);
watch(selectedTypeIds, saveFiltersConfig, { deep: true });

const isRetro = computed(() => dofusStore.currentGameVersion?.code === 'retro');
const isEnvCompatible = computed(() => isElectron && isLinux && isRetro.value);
const isSystemReady = computed(() => 
  hdvStore.systemStatus.tcpdumpInstalled && hdvStore.systemStatus.hasPermissions
);

watch(
  [() => dofusStore.currentGameVersionId, () => dofusStore.currentGameServerId],
  async () => {
    await store.loadFromDB();
  }
);
</script>

<template>
  <div class="container-fluid bank-management-page">
    <!-- Bloc Compatibilité -->
    <div class="container">
      <aside v-if="!isEnvCompatible" class="compatibility-warning">
        <header><strong>Accès restreint</strong></header>
        <ul class="warning-list">
          <li v-if="!isElectron">L'usage du sniffer nécessite l'application Desktop</li>
          <li v-else-if="!isLinux">Cette fonctionnalité est réservée à Linux</li>
          <li v-if="!isRetro">Le sniffer est configuré pour Dofus Retro uniquement</li>
        </ul>
      </aside>

      <aside v-else-if="hdvStore.systemStatus.checked && !isSystemReady" class="system-warning">
        <div class="warning-header">
          <strong>Dépendances système manquantes</strong>
          <button class="outline contrast refresh-btn" @click="hdvStore.checkSystem()" data-tooltip="Actualiser l'état">
            <span class="mdi mdi-refresh"></span>
          </button>
        </div>
        <ul class="warning-list">
          <li v-if="!hdvStore.systemStatus.tcpdumpInstalled" class="error">
            <span class="mdi mdi-close-circle-outline"></span> 
            <code>tcpdump</code> n'est pas installé.
          </li>
          <li v-if="!hdvStore.systemStatus.hasPermissions" class="error">
            <span class="mdi mdi-close-circle-outline"></span>
            Permissions insuffisantes.
          </li>
        </ul>
      </aside>
    </div>

    <!-- Header de la banque -->
    <header class="bank-header">
      <div class="left-tools">
        <div class="sniffer-status-badge" :class="{ active: store.isSniffing }">
          <span class="mdi" :class="store.isSniffing ? 'mdi-record-circle' : 'mdi-pause-circle'"></span>
          {{ store.isSniffing ? 'SCAN ACTIF' : 'SCAN PAUSE' }}
        </div>
        
        <div class="search-wrapper">
          <span class="mdi mdi-magnify search-icon"></span>
          <input 
            type="search" 
            v-model="searchQuery" 
            placeholder="Rechercher..." 
            class="search-input"
          />
        </div>

        <div class="filter-tools">
          <BankTypeFilterButton v-model="selectedTypeIds" />
          
          <div class="sort-wrapper">
            <select v-model="sortBy" class="sort-select">
              <option value="name">Nom</option>
              <option value="type">Type</option>
              <option value="level">Niveau</option>
              <option value="quantity">Quantité</option>
              <option value="price">Prix Total</option>
            </select>
            <button class="ghost-btn sort-order-btn" @click="toggleSortOrder">
              <span class="mdi" :class="sortOrder === 'asc' ? 'mdi-sort-ascending' : 'mdi-sort-descending'"></span>
            </button>
          </div>
        </div>
      </div>

      <div class="right-tools">
        <div class="bank-value-stats" v-if="store.items.length > 0">
          <div class="total-kamas">
            <span class="amount">{{ totalValueDisplay }}</span>
            <span class="currency">₭</span>
          </div>
          <div class="items-count-stats">
            <span class="count with-price" :title="bankStats.withPrice + ' items avec prix'">
              <span class="mdi mdi-tag-check-outline"></span> {{ bankStats.withPrice }}
            </span>
            <span class="count without-price" :title="bankStats.withoutPrice + ' items sans prix'">
              <span class="mdi mdi-tag-off-outline"></span> {{ bankStats.withoutPrice }}
            </span>
          </div>
        </div>

        <div class="bank-stats">
          <div class="slots-info">
            <strong>{{ store.items.length }}</strong> slots
          </div>
          <div class="sync-info">
            <span v-if="store.isSyncing" aria-busy="true">Sync...</span>
            <span v-else class="last-sync" :title="'Dernière récupération: ' + (store.lastSync ? new Date(store.lastSync).toLocaleString() : 'Jamais')">
              {{ lastSyncDisplay }}
            </span>
          </div>
        </div>

        <button 
          v-if="store.items.length > 0"
          class="ghost-btn btn-clear"
          @click="store.clearBank()"
          data-tooltip="Vider la banque"
        >
          <span class="mdi mdi-delete-sweep-outline"></span>
        </button>
      </div>
    </header>

    <hr />

    <!-- Grille des items -->
    <main class="bank-content">
      <div class="items-container">
        <div v-if="filteredAndSortedItems.length > 0" class="items-grid">
          <BankItemCard 
            v-for="item in filteredAndSortedItems" 
            :key="item.instanceId" 
            :bank-item="item" 
          />
        </div>

        <div v-else class="empty-bank">
          <div v-if="store.isSniffing && store.items.length === 0" class="sync-loader">
            <div class="status-indicator"></div>
            <p>En attente de paquets...</p>
            <small class="muted">Ouvrez votre banque en jeu pour commencer la capture</small>
          </div>
          <div v-else class="no-data">
            <span class="mdi mdi-bank-outline large-icon"></span>
            <p v-if="searchQuery || selectedTypeIds.length > 0">Aucun résultat pour vos filtres</p>
            <p v-else>Banque vide ou non capturée.</p>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped lang="scss">
.bank-management-page {
  padding: 1rem 0;
}

.bank-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1.5rem;
  padding: 0 1.5rem;
  margin-bottom: 0.5rem;
}

.left-tools {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex: 1;

  .sniffer-status-badge {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4rem 0.8rem;
    border-radius: 2rem;
    font-weight: 800;
    font-size: 0.65rem;
    white-space: nowrap;
    background: var(--pico-card-background-color);
    border: 1px solid var(--pico-muted-border-color);
    color: var(--pico-muted-color);
    &.active {
      color: var(--pico-ins-color);
      border-color: var(--pico-ins-color);
      .mdi { animation: blink 2s infinite; }
    }
  }

  .search-wrapper {
    position: relative;
    max-width: 240px;
    flex: 1;

    .search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      opacity: 0.5;
    }

    .search-input {
      padding-left: 2.5rem;
      margin-bottom: 0;
      height: 34px;
      font-size: 0.85rem;
    }
  }

  .filter-tools {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .sort-wrapper {
    display: flex;
    align-items: center;
    gap: 0.5rem;

    .sort-select {
      margin-bottom: 0;
      height: 34px;
      font-size: 0.85rem;
      padding: 0 2rem 0 0.75rem;
      width: 130px;
    }
  }
}

.right-tools {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  margin-left: auto;

  .bank-value-stats {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    line-height: 1.1;

    .total-kamas {
      font-weight: 800;
      font-size: 1.1rem;
      color: var(--pico-primary);
      .currency { font-size: 0.8rem; margin-left: 2px; }
    }

    .items-count-stats {
      display: flex;
      gap: 0.75rem;
      font-size: 0.65rem;
      font-weight: 600;
      color: var(--pico-muted-color);
      
      .count {
        display: flex;
        align-items: center;
        gap: 0.2rem;
        &.with-price .mdi { color: var(--pico-ins-color); }
        &.without-price .mdi { color: var(--pico-del-color); }
      }
    }
  }

  .bank-stats {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    line-height: 1.2;
    min-width: 80px;

    .slots-info {
      font-size: 0.8rem;
      color: var(--pico-color);
      strong { font-weight: 800; }
    }

    .sync-info {
      font-size: 0.65rem;
      font-weight: 600;
      color: var(--pico-muted-color);
      text-transform: uppercase;
      
      .last-sync {
        color: var(--pico-ins-color);
        cursor: help;
      }
    }
  }
}

.ghost-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  padding: 0;
  margin: 0;
  border-radius: var(--pico-border-radius);
  border: 1px solid transparent;
  background: transparent;
  color: var(--pico-muted-color);
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover {
    color: var(--pico-primary);
    background-color: var(--pico-card-background-color);
    border-color: var(--pico-muted-border-color);
  }

  .mdi { font-size: 1.2rem; }
}

.bank-content {
  display: flex;
  justify-content: center;
  padding: 1.5rem 0;
}

.items-container {
  width: 90%;
  max-width: 1400px;
}

.items-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(64px, 1fr));
  gap: 8px;
  justify-content: center;
}

.empty-bank {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 0;
  opacity: 0.6;
  text-align: center;
  .large-icon { font-size: 4rem; margin-bottom: 1rem; }
  .sync-loader {
    .status-indicator { width: 12px; height: 12px; background-color: var(--pico-ins-color); border-radius: 50%; margin: 0 auto 1.5rem; animation: blink 2s infinite ease-in-out; }
  }
}

.compatibility-warning, .system-warning {
  border: 1px solid var(--pico-form-element-invalid-border-color);
  background-color: var(--pico-card-background-color);
  padding: 1rem; margin-bottom: 1rem; border-radius: var(--pico-border-radius);
  .warning-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; color: var(--pico-del-color); }
  .warning-list { margin: 0; padding-left: 0; list-style: none; font-size: 0.9rem; li { display: flex; align-items: center; gap: 0.5rem; &.error { color: var(--pico-del-color); } } }
}

@keyframes blink { 0% { opacity: 0.4; } 50% { opacity: 1; } 100% { opacity: 0.4; } }
</style>
