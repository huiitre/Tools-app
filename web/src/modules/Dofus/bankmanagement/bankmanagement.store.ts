import { defineStore } from 'pinia';
import { useSnifferConfigStore } from '@/modules/Dofus/shared/snifferConfig.store';
import { useProxyConfigStore } from '@/modules/Dofus/shared/proxyConfig.store';
import { useDofusStore } from '@/modules/Dofus/dofus.store';
import { IndexedDBBankAdapter } from './infrastructure/IndexedDBBankAdapter';
import { SyncBankService } from './application/services/SyncBankService';
import type { BankItem } from './domain/BankItem';
import type { RawBankItem } from './application/dtos/RawBankItem';
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices';

const repository = new IndexedDBBankAdapter();
const syncService = new SyncBankService(repository);

export const useBankManagementStore = defineStore('bankmanagement', {
  state: () => ({
    items: [] as BankItem[],
    lastSync: null as number | null,
    isListenerActive: false,
    isSyncing: false,
    rawBuffer: [] as RawBankItem[],
    isFullDumpPending: false,
    syncTimeout: null as any | null
  }),

  getters: {
    isSniffing(): boolean {
      const sniffer = useSnifferConfigStore();
      const proxy = useProxyConfigStore();
      return (sniffer.isSniffing && sniffer.modules.bank) || (proxy.status.active && proxy.config.modules.bank);
    }
  },

  actions: {
    async init() {
      await this.loadFromDB();
      this.setupListener();
    },

    async loadFromDB() {
      const dofusStore = useDofusStore();
      const versionId = dofusStore.currentGameVersionId;
      const serverId = dofusStore.currentGameServerId;
      if (versionId && serverId) {
        this.items = await repository.getByServer(versionId, serverId);
        this.lastSync = await repository.getLastSync(versionId, serverId);
        const itemIds = this.items.map(i => i.metadata?.id).filter((id): id is number => id !== undefined);
        if (itemIds.length > 0) {
          const { load } = useItemPrices();
          await load(itemIds);
        }
      }
    },

    setupListener() {
      if (this.isListenerActive || !window.electron) return;
      
      window.electron.onBankFullDump(() => {
        if (!this.isSniffing) return;
        this.isFullDumpPending = true;
      });

      window.electron.onBankItemCaptured((item: RawBankItem) => {
        this._handleIncomingRawItems(item);
      });

      window.electron.onProxyBankOpened(() => {
        if (!this.isSniffing) return;
        this.isFullDumpPending = true;
      });

      window.electron.onProxyBankItems((items: RawBankItem[]) => {
        // Pour le proxy, on considère que la réception massive d'items EL est un dump
        if (items.length > 10) {
            this.isFullDumpPending = true;
        }
        this._handleIncomingRawItems(items);
      });
      
      this.isListenerActive = true;
    },

    _handleIncomingRawItems(items: RawBankItem | RawBankItem[]) {
      if (!this.isSniffing) return;
      const capturedItems = Array.isArray(items) ? items : [items];
      this.rawBuffer.push(...capturedItems);
      if (this.syncTimeout) clearTimeout(this.syncTimeout);
      this.syncTimeout = setTimeout(async () => {
        await this.triggerSync();
      }, 1500); // On augmente un peu pour laisser le temps au dump d'arriver
    },

    async triggerSync() {
      const dofusStore = useDofusStore();
      const versionId = dofusStore.currentGameVersionId;
      const serverId = dofusStore.currentGameServerId;
      if (!versionId || !serverId || this.rawBuffer.length === 0) return;

      this.isSyncing = true;
      try {
        await syncService.sync(this.rawBuffer, versionId, serverId, this.isFullDumpPending);
        const now = Date.now();
        await repository.saveLastSync(versionId, serverId, now);
        this.lastSync = now;
        await this.loadFromDB();
        this.rawBuffer = [];
        this.isFullDumpPending = false;
      } finally {
        this.isSyncing = false;
        this.syncTimeout = null;
      }
    },

    async clearBank() {
      const dofusStore = useDofusStore();
      if (dofusStore.currentGameVersionId && dofusStore.currentGameServerId) {
        await repository.clearServer(dofusStore.currentGameVersionId, dofusStore.currentGameServerId);
        this.items = [];
        this.lastSync = null;
      }
    }
  }
});
