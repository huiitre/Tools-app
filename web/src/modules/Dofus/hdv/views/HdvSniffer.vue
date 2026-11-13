<script setup lang="ts">
import { computed, ref } from 'vue';
import { useHdvSnifferStore } from '../hdvSniffer.store';
import HdvSnifferRow from '../components/HdvSnifferRow.vue';
import HdvBatchRow from '../components/HdvBatchRow.vue';

//* Journal des prix capturés. La réception et l'envoi à l'API tournent en tâche de fond
//* (useDofusBackgroundSession), que cette page soit ouverte ou non.
const hdvStore = useHdvSnifferStore();

const ignoredLabel = computed(() => {
  const ignored = hdvStore.lastIgnored;
  if (!ignored) return null;
  const game = ignored.source === 'dofus3' ? 'Dofus 3' : 'Dofus Retro';
  const time = new Date(ignored.at).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  return `${ignored.count} prix ${game} ignorés à ${time} : le sélecteur de jeu n'est pas sur ${game}, ou aucun serveur n'est choisi.`;
});

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
</script>

<template>
  <div class="container-fluid hdv-sniffer-page">
    <header class="hdv-header sniffer-hero-header">
      <div class="container hero-content">
        <hgroup class="text-center">
          <h1>Hôtel de Vente</h1>
          <p>Scan automatique des prix par catégories</p>
        </hgroup>
        <p class="source-hint">
          Dofus Retro : prix capturés par le service MitM du header.<br />
          Dofus 3 : activer le Sniffer HDV depuis le bouton Capture Dofus 3 du header.
        </p>
        <p v-if="ignoredLabel" class="ignored-hint">
          <span class="mdi mdi-alert-outline" /> {{ ignoredLabel }}
        </p>

        <div class="main-controls">
          <div v-if="hdvStore.scanProgress" class="scan-progress-container">
             <div class="progress-info">
                <span>{{ hdvStore.isSyncing ? 'Synchronisation...' : 'Scan en cours...' }}</span>
                <strong>{{ hdvStore.scanProgress.current }} / {{ hdvStore.scanProgress.total }}</strong>
             </div>
             <progress :value="progressPercent" max="100" class="mini-progress"></progress>
          </div>

          <div v-if="hdvStore.captures.length > 0" class="status-summary">
             <span class="badge contrast">{{ hdvStore.captures.length }} captures en session</span>
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
          <template v-for="capture in hdvStore.captures" :key="capture.id">
            <HdvBatchRow
              v-if="capture.kind === 'batch'"
              :capture="capture"
              :is-expanded="expandedCaptureId === capture.id"
              @toggle="toggleExpand(capture.id)"
            />
            <HdvSnifferRow
              v-else
              :capture-id="capture.id"
              :item="hdvStore.metadataFor(capture)"
              :item-id="capture.itemId"
              :instances="capture.instances"
              :average-price="capture.averagePrice"
              :is-expanded="expandedCaptureId === capture.id"
              @toggle="toggleExpand(capture.id)"
            />
          </template>
        </div>
        <div v-else class="empty-state">
          <div class="status-indicator"></div>
          <h3>Prêt pour le scan</h3>
          <p class="muted">Les prix apparaîtront ici dès qu'ils seront capturés, depuis Dofus Retro ou Dofus 3.</p>
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
.source-hint { margin: 0; text-align: center; font-size: 0.75rem; color: var(--pico-muted-color); line-height: 1.5; }
.ignored-hint { margin: 0; font-size: 0.75rem; color: var(--pico-del-color); }
</style>
