import { defineStore } from 'pinia';
import { toRaw } from 'vue';

export interface ProxyConfig {
  remoteIp: string;
  remotePort: string;
  localPort: string;
  forceManual: boolean;
  modules: {
    hdvAuto: boolean;
    hdvManual: boolean;
    bank: boolean;
  };
}

export interface ProxyStatus {
  active: boolean;
  connections: number;
  config: any;
}

export const useProxyConfigStore = defineStore('proxyConfig', {
  state: () => ({
    config: {
      remoteIp: '34.253.140.241',
      remotePort: '443',
      localPort: '5558',
      forceManual: false,
      modules: {
        hdvAuto: true,
        hdvManual: true,
        bank: true
      }
    } as ProxyConfig,
    status: {
      active: false,
      connections: 0,
      config: null
    } as ProxyStatus,
    candidates: [] as any[],
    isScanning: false
  }),

  actions: {
    hydrate() {
      const stored = localStorage.getItem('proxy.config');
      if (stored) {
        const parsed = JSON.parse(stored);
        this.config = { ...this.config, ...parsed, modules: { ...this.config.modules, ...(parsed.modules || {}) } };
      }
      
      if (window.electron) {
        window.electron.onProxyStatus((status: ProxyStatus) => {
          this.status = status;
          if (status.active && status.config) {
            this.config.remoteIp = status.config.remoteIp;
          }
        });
      }
    },

    async toggleProxy() {
      if (!window.electron) return;
      
      if (this.status.active) {
        await window.electron.stopProxy();
      } else {
        const config = toRaw(this.config);
        const result = await window.electron.startProxy({
            remoteIp: config.remoteIp,
            remotePort: parseInt(config.remotePort),
            localPort: parseInt(config.localPort),
            manualMode: config.forceManual,
            modules: config.modules
        });
        if (!result.success && result.error) {
            console.error('[ProxyStore] Erreur démarrage:', result.error);
        }
      }
    },

    saveConfig() {
      localStorage.setItem('proxy.config', JSON.stringify(this.config));
      if (this.status.active && window.electron && window.electron.updateProxyModules) {
        window.electron.updateProxyModules(toRaw(this.config.modules));
      }
    },

    async scan() {
      if (!window.electron) return;
      this.isScanning = true;
      try {
        this.candidates = await window.electron.detectCandidates();
      } finally {
        this.isScanning = false;
      }
    }
  }
});
