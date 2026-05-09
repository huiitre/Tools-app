import { defineStore } from 'pinia';
import { toRaw } from 'vue';

export interface SnifferConfig {
  remoteIp: string;
  remotePort: string;
  localPort: string;
  forceManual: boolean;
}

export interface SnifferCandidate {
  ip: string;
  port: string;
  localPort: string;
  processName: string;
  isRecommended: boolean;
}

export const useSnifferConfigStore = defineStore('snifferConfig', {
  state: () => ({
    config: {
      remoteIp: '',
      remotePort: '',
      localPort: '',
      forceManual: false
    } as SnifferConfig,
    activeConfig: null as { remoteIp: string; remotePort: string; localPort?: string } | null,
    modules: {
      hdv: true,
      bank: true
    },
    isSniffing: false,
    candidates: [] as SnifferCandidate[],
    isScanning: false,
    showModal: false
  }),

  actions: {
    hydrate() {
      const stored = localStorage.getItem('sniffer.config');
      if (stored) {
        this.config = JSON.parse(stored);
      }
      const storedModules = localStorage.getItem('sniffer.modules');
      if (storedModules) {
        this.modules = JSON.parse(storedModules);
      }
    },

    async fetchActiveConfig() {
      if (!window.electron) return;
      this.activeConfig = await window.electron.getActiveConfig();
    },

    async toggleSniffer() {
      if (!window.electron) return;
      
      if (this.isSniffing) {
        await window.electron.stopSniffing();
        this.isSniffing = false;
      } else {
        const forcedConfig = this.getForcedConfig();
        // Utilisation de toRaw() pour supprimer le Proxy Vue avant l'envoi IPC
        await window.electron.updateSnifferModules(toRaw(this.modules));
        const result = await window.electron.startSniffing(toRaw(forcedConfig));
        if (result.success) {
          this.isSniffing = true;
          await this.fetchActiveConfig();
        }
      }
    },

    async toggleModule(module: 'hdv' | 'bank') {
      this.modules[module] = !this.modules[module];
      localStorage.setItem('sniffer.modules', JSON.stringify(this.modules));
      
      if (window.electron) {
        await window.electron.updateSnifferModules(toRaw(this.modules));
      }
    },

    saveConfig(ip: string, port: string, localPort: string, force: boolean) {
      this.config = { remoteIp: ip, remotePort: port, localPort, forceManual: force };
      localStorage.setItem('sniffer.config', JSON.stringify(this.config));
    },

    async scan() {
      if (!window.electron) return;
      this.isScanning = true;
      try {
        this.candidates = await window.electron.detectCandidates();
      } finally {
        this.isScanning = false;
      }
    },

    getForcedConfig() {
      if (this.config.forceManual && this.config.remoteIp && this.config.remotePort) {
        return { 
          remoteIp: this.config.remoteIp, 
          remotePort: this.config.remotePort,
          localPort: this.config.localPort
        };
      }
      return null;
    }
  }
});
