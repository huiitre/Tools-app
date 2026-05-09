<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useHdvSnifferStore } from '../hdvSniffer.store';
import { useDofusStore } from '@/modules/Dofus/dofus.store';
import { useOS } from '@/composables/useOS';
import { useEnv } from '@/composables/useEnv';
import HdvSnifferRow from '../components/HdvSnifferRow.vue';

const hdvStore = useHdvSnifferStore();
const dofusStore = useDofusStore();
const { isLinux } = useOS();
const { isElectron } = useEnv();

const isRetro = computed(() => dofusStore.currentGameVersion?.code === 'retro');
const isEnvCompatible = computed(() => isElectron && isLinux && isRetro.value);

const expandedCaptureId = ref<string | null>(null);

const toggleExpand = (captureId: string) => {
  if (expandedCaptureId.value === captureId) {
    expandedCaptureId.value = null;
  } else {
    expandedCaptureId.value = captureId;
  }
};

const progressPercent = computed(() => {
    if (!hdvStore.scanProgress || hdvStore.scanProgress.total === 0) return 0;
    return Math.round((hdvStore.scanProgress.current / hdvStore.scanProgress.total) * 100);
});

onMounted(() => {
  if (isEnvCompatible.value) {
    hdvStore.setupListener();
  }
});
</script>

<template>
  <div class="container-fluid hdv-sniffer-page">
    <header class="hdv-header sniffer-hero-header">
      <div class="container hero-content">
        <hgroup class="text-center">
          <h1>Hôtel de Vente</h1>
          <p>Scan automatique des prix par catégories</p>
        </hgroup>

        <div class="main-controls">
          <div v-if="hdvStore.scanProgress" class="scan-progress-container">
             <div class="progress-info">
                <span>{{ hdvStore.isSyncing ? 'Synchronisation...' : 'Scan en cours...' }}</span>
                <strong>{{ hdvStore.scanProgress.current }} / {{ hdvStore.scanProgress.total }}</strong>
             </div>
             <progress :value="progressPercent" max="100" class="mini-progress"></progress>
          </div>

          <div v-if="hdvStore.captures.length > 0" class="status-summary">
             <span class="badge contrast">{{ hdvStore.captures.length }} items en session</span>
             <button 
                class="outline contrast btn-clear" 
                :disabled="!!hdvStore.scanProgress"
                @click="hdvStore.clearDisplay()"
             >
                <span class="mdi mdi-delete-sweep-outline"></span> 
                {{ hdvStore.scanProgress ? 'SCAN EN COURS...' : 'VIDER LA LISTE' }}
             </button>
          </div>
        </div>
      </div>
    </header>

    <main class="results-main">
      <div class="container results-container">
        <div v-if="hdvStore.captures.length > 0" class="results-list">
          <HdvSnifferRow 
            v-for="capture in hdvStore.captures" 
            :key="capture.id"
            :capture-id="capture.id"
            :item="hdvStore.itemsMetadata[capture.itemId]"
            :item-id="capture.itemId"
            :instances="capture.instances"
            :average-price="capture.averagePrice"
            :is-expanded="expandedCaptureId === capture.id"
            @toggle="toggleExpand(capture.id)"
          />
        </div>
        <div v-else class="empty-state">
          <div class="status-indicator"></div>
          <h3>Prêt pour le scan</h3>
          <p class="muted">Sélectionnez une catégorie en HDV pour lancer la capture automatique.</p>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped lang="scss">
.hdv-sniffer-page { display: flex; flex-direction: column; }
.hdv-header {
  background-color: var(--pico-background-color);
  border-bottom: 1px solid var(--pico-muted-border-color);
  padding: 1.5rem 0;
  display: flex;
  justify-content: center;

  &.sniffer-hero-header .hero-content { display: flex; flex-direction: column; align-items: center; gap: 1rem; hgroup { margin: 0; } }
}
.main-controls { display: flex; align-items: center; gap: 1.5rem; width: 100%; justify-content: center; }
.scan-progress-container {
    flex: 1; max-width: 400px;
    .progress-info { display: flex; justify-content: space-between; font-size: 0.8rem; margin-bottom: 0.4rem; span { color: var(--pico-primary); font-weight: 600; } }
    progress { margin-bottom: 0; height: 8px; }
}
.status-summary { display: flex; align-items: center; gap: 1rem; .badge { padding: 0.4rem 1rem; border-radius: 2rem; font-weight: 700; } }
.btn-clear { margin: 0; padding: 0.4rem 1rem; font-size: 0.75rem; font-weight: 800; border-radius: 2rem; display: flex; align-items: center; gap: 0.3rem; }
.results-main { padding: 2rem 0; }
.results-container { max-width: 900px; margin: 0 auto; }
.results-list { display: flex; flex-direction: column; gap: 0.5rem; }
.text-center { text-align: center; }
.empty-state {
  display: flex; flex-direction: column; align-items: center; padding: 4rem 2rem; text-align: center; border-radius: var(--pico-border-radius); background-color: var(--pico-card-background-color);
  .status-indicator { width: 12px; height: 12px; background-color: var(--pico-primary); border-radius: 50%; margin-bottom: 1.5rem; animation: blink 2s infinite ease-in-out; }
}
@keyframes blink { 0% { opacity: 0.4; } 50% { opacity: 1; } 100% { opacity: 0.4; } }
.muted { color: var(--pico-muted-color); }
</style>
