<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useFloating, offset, autoUpdate } from '@floating-ui/vue';
import { useProxyConfigStore } from '../proxyConfig.store';
import { useEnv } from '@/composables/useEnv';

const configStore = useProxyConfigStore();
const { isElectron } = useEnv();

const isOpen = ref(false);
const reference = ref<HTMLElement | null>(null);
const floating = ref<HTMLElement | null>(null);

const { floatingStyles } = useFloating(reference, floating, {
  placement: 'bottom-end',
  middleware: [offset(5)],
  whileElementsMounted: autoUpdate,
});

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (reference.value && floating.value && !reference.value.contains(target) && !floating.value.contains(target)) {
    isOpen.value = false;
  }
};

const onScroll = () => {
  if (isOpen.value) {
    isOpen.value = false;
  }
};

const toggleOpen = async () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    configStore.hydrate();
    configStore.scan();
  }
};

const selectCandidate = (c: any) => {
  configStore.config.remoteIp = c.ip;
  configStore.config.remotePort = c.port.toString();
  configStore.config.forceManual = true;
  configStore.saveConfig();
};

onMounted(() => {
  document.addEventListener('click', onClickOutside);
  window.addEventListener('scroll', onScroll, true); // Capture scroll even on containers
});

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside);
  window.removeEventListener('scroll', onScroll, true);
});

watch(() => configStore.config.modules, () => {
    configStore.saveConfig();
}, { deep: true });

watch(() => configStore.config.forceManual, () => {
    configStore.saveConfig();
});
</script>

<template>
  <div v-if="isElectron" class="proxy-config-container">
    <button
      ref="reference"
      class="proxy-config-button"
      type="button"
      aria-label="Configuration Proxy"
      title="Intercepteur de paquets (MitM)"
      @click="toggleOpen"
    >
      <i class="mdi mdi-server-network" :class="{ active: configStore.status.active }" />
    </button>

    <div
      v-if="isOpen"
      ref="floating"
      :style="floatingStyles"
      class="floating-panel"
    >
      <div class="proxy-panel">
        <header class="main-header">
          <strong>Service MitM</strong>
          <button 
            :class="configStore.status.active ? 'secondary' : 'primary'"
            @click="configStore.toggleProxy"
            class="global-toggle"
          >
            <span class="mdi" :class="configStore.status.active ? 'mdi-stop' : 'mdi-play'"></span>
            {{ configStore.status.active ? 'STOP' : 'START' }}
          </button>
        </header>

        <section class="status-section" v-if="configStore.status.active">
            <div class="status-row">
                <span>Connexions :</span>
                <span class="count">{{ configStore.status.connections }}</span>
            </div>
            <div class="status-row">
                <span>Cible :</span>
                <span class="target">{{ configStore.status.config?.remoteIp || 'Inconnu' }}</span>
            </div>
        </section>

        <hr />

        <section class="modules-section">
            <header>Modules Actifs</header>
            <div class="switches-list">
                <label class="switch-label">
                    <input type="checkbox" v-model="configStore.config.modules.hdvAuto" role="switch" />
                    HDV Automatique
                </label>
                <label class="switch-label">
                    <input type="checkbox" v-model="configStore.config.modules.hdvManual" role="switch" />
                    HDV Manuel (Clic)
                </label>
                <label class="switch-label">
                    <input type="checkbox" v-model="configStore.config.modules.bank" role="switch" />
                    Gestion Banque
                </label>
            </div>
        </section>

        <hr />

        <section class="config-form">
          <label class="switch-label header-switch">
            <input type="checkbox" v-model="configStore.config.forceManual" role="switch" />
            Réseau Manuel
          </label>

          <div class="inputs-grid" v-if="configStore.config.forceManual">
            <div class="input-group">
                <span class="label">IP DISTANTE</span>
                <input 
                type="text" 
                v-model="configStore.config.remoteIp" 
                placeholder="Ex: 34.240.x.x" 
                />
            </div>
            <div class="ports-row">
              <div class="input-group">
                <span class="label">PORT CIBLE</span>
                <input type="text" v-model="configStore.config.remotePort" placeholder="443" />
              </div>
              <div class="input-group">
                <span class="label">PORT LOCAL</span>
                <input type="text" v-model="configStore.config.localPort" placeholder="5558" />
              </div>
            </div>
            <button @click="configStore.saveConfig()" class="save-btn">
                APPLIQUER CONFIG
            </button>
          </div>
          <div v-else class="auto-badge">
            <i class="mdi mdi-auto-fix"></i>
            Plages d'IP Ankama Auto
          </div>
        </section>

        <hr />

        <section class="scan-section">
          <div class="scan-header">
            <span>Détection Dofus</span>
            <i class="mdi mdi-refresh" :class="{ 'fa-spin': configStore.isScanning }" @click="configStore.scan"></i>
          </div>

          <div class="candidates-list" v-if="configStore.candidates.length > 0">
            <div 
              v-for="c in configStore.candidates" 
              :key="c.ip + c.port" 
              class="candidate-item"
              @click="selectCandidate(c)"
            >
              <div class="candidate-info">
                <span class="process">{{ c.processName }}</span>
                <span class="address">{{ c.ip }}:{{ c.port }}</span>
              </div>
              <i v-if="c.isRecommended" class="mdi mdi-check-decagram recommended"></i>
            </div>
          </div>
          <p v-else-if="!configStore.isScanning" class="no-data">Aucune connexion</p>
        </section>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.proxy-config-container { display: inline-flex; }
.proxy-config-button {
  display: inline-flex; align-items: center; justify-content: center;
  width: 2.25rem; height: 2.25rem; padding: 0; margin: 0;
  border-radius: var(--pico-border-radius); border: 1px solid transparent; background: transparent;
  color: var(--pico-muted-color); cursor: pointer; transition: all 0.15s ease;
  &:hover { color: var(--pico-primary); background-color: var(--pico-card-background-color); border-color: var(--pico-muted-border-color); }
  i { font-size: 1.2rem; &.active { color: var(--pico-primary); } }
}
.floating-panel {
  position: absolute; z-index: 1000; width: 260px; padding: 0.75rem;
  background: var(--pico-background-color); border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius); box-shadow: var(--pico-card-box-shadow); font-size: 0.75rem;
}
.proxy-panel {
  .main-header {
    display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;
    strong { text-transform: uppercase; letter-spacing: 0.05em; font-weight: 700; }
    .global-toggle { margin: 0; padding: 0.2rem 0.6rem; font-size: 0.65rem; font-weight: 800; width: auto; display: flex; align-items: center; gap: 0.3rem; border-radius: 4px; }
  }
  .status-section {
    display: flex; flex-direction: column; gap: 0.4rem; margin-bottom: 0.5rem; padding: 0.5rem;
    background: var(--pico-card-background-color); border-radius: 4px;
    .status-row { display: flex; justify-content: space-between; span:first-child { color: var(--pico-muted-color); } .count, .target { font-weight: 600; font-family: monospace; } }
  }
  .modules-section {
    header { color: var(--pico-muted-color); font-weight: 700; margin-bottom: 0.5rem; text-transform: uppercase; font-size: 0.65rem; }
    .switches-list { display: flex; flex-direction: column; gap: 0.4rem; }
  }
  .switch-label {
    display: flex; align-items: center; gap: 0.5rem; cursor: pointer; font-size: 0.7rem;
    input { margin: 0; scale: 0.7; }
    &.header-switch { margin-bottom: 0.5rem; color: var(--pico-muted-color); font-weight: 700; text-transform: uppercase; font-size: 0.65rem; }
  }
  .auto-badge {
    display: flex; align-items: center; justify-content: center; gap: 0.5rem; padding: 0.4rem;
    background: var(--pico-card-background-color); border-radius: 4px; color: var(--pico-primary); font-weight: 600; font-size: 0.65rem;
  }
  .inputs-grid {
    display: flex; flex-direction: column; gap: 0.6rem;
    .input-group {
        display: flex; flex-direction: column; gap: 0.2rem;
        .label { font-size: 0.55rem; font-weight: 800; color: var(--pico-muted-color); text-transform: uppercase; padding-left: 0.2rem; }
    }
    .ports-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.4rem; }
    input { height: 26px; padding: 0 0.5rem; font-size: 0.7rem; margin: 0; }
    .save-btn { height: 26px; padding: 0; font-size: 0.6rem; margin-top: 0.2rem; font-weight: 700; }
  }
  hr { margin: 0.75rem 0; opacity: 0.4; }
  .scan-section {
    .scan-header { display: flex; justify-content: space-between; align-items: center; color: var(--pico-muted-color); margin-bottom: 0.5rem; font-weight: 700; text-transform: uppercase; font-size: 0.65rem; i { font-size: 0.9rem; cursor: pointer; &:hover { color: var(--pico-primary); } } }
    .candidates-list { display: flex; flex-direction: column; gap: 0.25rem; max-height: 120px; overflow-y: auto; }
    .candidate-item {
      display: flex; justify-content: space-between; align-items: center; padding: 0.3rem 0.5rem; border-radius: 4px; background: var(--pico-card-background-color); cursor: pointer;
      &:hover { background: var(--pico-muted-border-color); }
      .candidate-info { display: flex; flex-direction: column; .process { font-weight: 600; font-size: 0.65rem; } .address { font-size: 0.6rem; opacity: 0.7; } }
      .recommended { color: var(--pico-primary); font-size: 0.8rem; }
    }
    .no-data { text-align: center; opacity: 0.5; margin: 0.5rem 0; }
  }
}
.fa-spin { animation: fa-spin 2s infinite linear; }
@keyframes fa-spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(359deg); } }
</style>
