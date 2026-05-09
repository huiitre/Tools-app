import { defineStore } from 'pinia';
import { useFetchItemsByAssetIds, useMutationItemPrices } from '@/modules/Dofus/item/fetch/item.fetch';
import { ItemLight } from '@/modules/Dofus/item/types/item.types';
import { calculateAverageUnitPrice } from './utils/priceAverageCalculator';
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices';

export interface SnifferCapture {
  id: string;
  itemId: number;
  instances: any[];
  averagePrice: number;
  timestamp: number;
}

export interface ScanProgress {
  current: number;
  total: number;
  remaining: number;
}

export const useHdvSnifferStore = defineStore('hdvSniffer', {
  state: () => ({
    captures: [] as SnifferCapture[], 
    itemsMetadata: {} as Record<number, ItemLight>,
    failedAssetIds: new Set<number>(),
    pendingPrices: new Map<number, number>(), // assetId -> price
    scanProgress: null as ScanProgress | null,
    isSyncing: false,
    isListenerActive: false,
    fetchTimeout: null as any | null,
    // RESTAURATION DE systemStatus POUR LA BANQUE
    systemStatus: {
      tcpdumpInstalled: true,
      hasPermissions: true,
      checked: false
    }
  }),

  actions: {
    async checkSystem() {
      const status = await window.electron?.checkSnifferRequirements();
      if (status) {
        this.systemStatus = { ...status, checked: true };
      }
      return this.systemStatus;
    },

    setupListener() {
      if (this.isListenerActive || !window.electron) return;
      
      window.electron.onProxyHdvPrices((data: any[]) => {
        if (!data || data.length === 0) return;
        const assetId = data[0].itemId;
        const averagePrice = calculateAverageUnitPrice(data);
        
        const capture = { 
            id: `${assetId}-${Date.now()}-${Math.random()}`, 
            itemId: assetId, 
            instances: data, 
            averagePrice, 
            timestamp: Date.now() 
        };
        
        this.captures.unshift(capture);
        if (this.captures.length > 200) this.captures.pop();

        if (!this.failedAssetIds.has(assetId)) {
            this.pendingPrices.set(assetId, averagePrice);
            this._debounceSync();
        }
      });

      window.electron.onProxyScanProgress((progress: ScanProgress) => {
        this.scanProgress = progress;
        if (progress.remaining === 0) {
            this._debounceSync(true);
            setTimeout(() => {
                if (this.scanProgress && this.scanProgress.remaining === 0) {
                    this.scanProgress = null;
                }
            }, 3000);
        }
      });

      this.isListenerActive = true;
    },

    _debounceSync(force = false) {
      if (this.fetchTimeout) clearTimeout(this.fetchTimeout);
      this.fetchTimeout = setTimeout(() => this.syncAll(), force ? 500 : 3000);
    },

    async syncAll() {
      if (this.isSyncing || this.pendingPrices.size === 0) return;
      this.isSyncing = true;

      try {
        const assetIds = Array.from(this.pendingPrices.keys());
        const prices = Array.from(this.pendingPrices.entries());

        const missingIds = assetIds.filter(id => !this.itemsMetadata[id] && !this.failedAssetIds.has(id));
        
        if (missingIds.length > 0) {
          try {
            const { data } = await useFetchItemsByAssetIds(missingIds);
            const foundIds = new Set(data.map(item => item.assetId));
            data.forEach(item => this.itemsMetadata[item.assetId] = item);
            
            missingIds.forEach(id => {
                if (!foundIds.has(id)) {
                    this.failedAssetIds.add(id);
                    this.pendingPrices.delete(id);
                }
            });
          } catch (e) {
            console.error('[SnifferStore] Metadata fetch error:', e);
          }
        }

        const priceBatch = prices
          .map(([assetId, price]) => {
            const meta = this.itemsMetadata[assetId];
            return meta ? { itemId: meta.id, price } : null;
          })
          .filter((p): p is { itemId: number; price: number } => p !== null);

        if (priceBatch.length > 0) {
          await useMutationItemPrices(priceBatch);
          const { refreshRecursive } = useItemPrices();
          await refreshRecursive(priceBatch.map(p => p.itemId));
          
          priceBatch.forEach(p => {
             const assetId = assetIds.find(id => this.itemsMetadata[id]?.id === p.itemId);
             if (assetId) this.pendingPrices.delete(assetId);
          });
        }
      } catch (e) {
        console.error('[SnifferStore] Sync Error:', e);
      } finally {
        this.isSyncing = false;
        if (this.pendingPrices.size > 0) this._debounceSync();
      }
    },

    clear() {
      this.captures = [];
      this.pendingPrices.clear();
      this.scanProgress = null;
    },

    clearDisplay() {
        this.captures = [];
    }
  }
});
