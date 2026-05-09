<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, computed } from 'vue';
import { useFloating, offset, autoUpdate } from '@floating-ui/vue';
import { useSnifferConfigStore, type SnifferCandidate } from '../snifferConfig.store';
import { useEnv } from '@/composables/useEnv';

const configStore = useSnifferConfigStore();
const { isElectron } = useEnv();

const isOpen = ref(false);
const reference = ref<HTMLElement | null>(null);
const floating = ref<HTMLElement | null>(null);

const localConfig = ref({ ...configStore.config });

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

const toggleOpen = async () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    configStore.hydrate();
    await configStore.fetchActiveConfig();
    
    localConfig.value = { ...configStore.config };

    if (!localConfig.value.forceManual && configStore.activeConfig) {
      localConfig.value.remoteIp = configStore.activeConfig.remoteIp;
      localConfig.value.remotePort = configStore.activeConfig.remotePort;
      localConfig.value.localPort = configStore.activeConfig.localPort || '';
    }

    configStore.scan();
  }
};

const selectCandidate = (c: SnifferCandidate) => {
  localConfig.value.remoteIp = c.ip;
  localConfig.value.remotePort = c.port;
  localConfig.value.localPort = c.localPort;
  localConfig.value.forceManual = true;
};

const handleSave = async () => {
  configStore.saveConfig(
    localConfig.value.remoteIp, 
    localConfig.value.remotePort, 
    localConfig.value.localPort, 
    localConfig.value.forceManual
  );
  
  if (configStore.isSniffing && window.electron) {
    const forcedConfig = configStore.getForcedConfig();
    await window.electron.startSniffing(forcedConfig);
  }
  
  isOpen.value = false;
};

onMounted(() => {
  document.addEventListener('click', onClickOutside);
});

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside);
});
</script>

<template>
  <div v-if="isElectron" class="sniffer-config-container">
    <button
      ref="reference"
      class="sniffer-config-button"
      type="button"
      aria-label="Configuration Sniffer"
      title="Sniffer"
      @click="toggleOpen"
    >
      <i class="mdi mdi-satellite-variant" :class="{ active: configStore.isSniffing }" />
    </button>

    <div
      v-if="isOpen"
      ref="floating"
      :style="floatingStyles"
      class="floating-panel"
    >
      <div class="sniffer-panel">
        <header class="main-header">
          <strong>Sniffer Global</strong>
          <button 
            :class="configStore.isSniffing ? 'secondary' : 'primary'"
            @click="configStore.toggleSniffer"
            class="global-toggle"
          >
            <span class="mdi" :class="configStore.isSniffing ? 'mdi-stop' : 'mdi-play'"></span>
            {{ configStore.isSniffing ? 'STOP' : 'START' }}
          </button>
        </header>

        <section class="modules-section">
          <div class="module-row">
            <span>HDV Sniffer</span>
            <input type="checkbox" role="switch" :checked="configStore.modules.hdv" @change="configStore.toggleModule('hdv')" />
          </div>
          <div class="module-row">
            <span>Bank Management</span>
            <input type="checkbox" role="switch" :checked="configStore.modules.bank" @change="configStore.toggleModule('bank')" />
          </div>
        </section>

        <hr />

        <section class="config-form">
          <label class="switch-label">
            <input type="checkbox" v-model="localConfig.forceManual" role="switch" />
            Forcer IP/Port
          </label>

          <div class="inputs-grid">
            <input 
              type="text" 
              v-model="localConfig.remoteIp" 
              placeholder="IP" 
              :readonly="!localConfig.forceManual"
              :class="{ disabled: !localConfig.forceManual }"
            />
            <div class="ports-row">
              <input 
                type="text" 
                v-model="localConfig.remotePort" 
                placeholder="Distant" 
                :readonly="!localConfig.forceManual"
                :class="{ disabled: !localConfig.forceManual }"
              />
              <input 
                type="text" 
                v-model="localConfig.localPort" 
                placeholder="Local" 
                :readonly="!localConfig.forceManual"
                :class="{ disabled: !localConfig.forceManual }"
              />
            </div>
          </div>

          <button @click="handleSave" class="save-btn" :disabled="localConfig.forceManual && (!localConfig.remoteIp || !localConfig.remotePort)">
            Appliquer
          </button>
        </section>

        <hr />

        <section class="scan-section">
          <div class="scan-header">
            <span>Détection</span>
            <i class="mdi mdi-refresh" :class="{ 'fa-spin': configStore.isScanning }" @click="configStore.scan"></i>
          </div>

          <div class="candidates-list" v-if="configStore.candidates.length > 0">
            <div 
              v-for="c in configStore.candidates" 
              :key="c.ip + c.port + c.localPort" 
              class="candidate-item"
              @click="selectCandidate(c)"
            >
              <div class="candidate-info">
                <span class="process">{{ c.processName }}</span>
                <span class="address">{{ c.ip }}:{{ c.port }} <small>(Local: {{ c.localPort }})</small></span>
              </div>
              <i v-if="c.isRecommended" class="mdi mdi-check-decagram recommended" title="Recommandé"></i>
            </div>
          </div>
          <p v-else-if="!configStore.isScanning" class="no-data">Aucune connexion</p>
        </section>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.sniffer-config-container {
  display: inline-flex;
}

.sniffer-config-button {
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

  i {
    font-size: 1.2rem;
    line-height: 1;
    &.active { color: var(--pico-primary); }
  }
}

.floating-panel {
  position: absolute;
  z-index: 1000;
  width: 260px;
  padding: 0.75rem;
  background: var(--pico-background-color);
  border: 1px solid var(--pico-muted-border-color);
  border-radius: var(--pico-border-radius);
  box-shadow: var(--pico-card-box-shadow);
  font-size: 0.75rem;
}

.sniffer-panel {
  .main-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    strong { text-transform: uppercase; letter-spacing: 0.05em; font-weight: 700; }
    
    .global-toggle {
      margin: 0;
      padding: 0.2rem 0.6rem;
      font-size: 0.65rem;
      font-weight: 800;
      width: auto;
      display: flex;
      align-items: center;
      gap: 0.3rem;
      border-radius: 4px;
    }
  }

  .modules-section {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    .module-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-weight: 500;
      input { margin: 0; scale: 0.8; transform-origin: right; }
    }
  }

  .config-form {
    .switch-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
      font-size: 0.7rem;
      cursor: pointer;
      input { margin: 0; scale: 0.8; }
    }

    .inputs-grid {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      margin-bottom: 0.5rem;

      .ports-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.4rem;
      }

      input { 
        height: 28px; 
        padding: 0 0.5rem; 
        font-size: 0.7rem;
        margin: 0;
        
        &.disabled {
          background-color: var(--pico-form-element-disabled-background-color);
          border-color: var(--pico-form-element-disabled-border-color);
          opacity: 0.6;
          cursor: not-allowed;
        }
      }
    }

    .save-btn {
      width: 100%;
      height: 28px;
      padding: 0;
      font-size: 0.7rem;
      margin: 0;
    }
  }

  hr { margin: 0.75rem 0; opacity: 0.5; }

  .scan-section {
    .scan-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      color: var(--pico-muted-color);
      margin-bottom: 0.5rem;
      font-weight: 600;
      i { 
        font-size: 1rem;
        cursor: pointer; 
        transition: color 0.2s;
        &:hover { color: var(--pico-primary); } 
      }
    }

    .candidates-list {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      max-height: 200px;
      overflow-y: auto;
      overscroll-behavior: contain;
    }

    .candidate-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.4rem 0.5rem;
      border-radius: 4px;
      background: var(--pico-card-background-color);
      cursor: pointer;
      transition: background 0.1s;

      &:hover { background: var(--pico-muted-border-color); }

      .candidate-info {
        display: flex;
        flex-direction: column;
        .process { font-weight: 600; font-size: 0.65rem; }
        .address { font-size: 0.6rem; opacity: 0.7; }
      }
      .recommended { color: var(--pico-primary); font-size: 0.9rem; }
    }

    .no-data { text-align: center; opacity: 0.5; margin: 0.5rem 0; }
  }
}

.fa-spin {
  animation: fa-spin 2s infinite linear;
}

@keyframes fa-spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(359deg); }
}
</style>
