import { defineStore } from 'pinia';
import { markRaw } from 'vue';
import { useFetchItemsByAssetIds, useMutationItemPrices } from '@/modules/Dofus/item/fetch/item.fetch';
import { ItemLight } from '@/modules/Dofus/item/types/item.types';
import { calculateAverageUnitPrice } from './utils/priceAverageCalculator';
import { useItemPrices } from '@/modules/Dofus/almanax/composables/useItemPrices';
import { useDofusStore } from '@/modules/Dofus/dofus.store';
import type { CaptureItemOffers, CapturePriceList } from '@/modules/Dofus/capture/capture.types';

//* Prix capturés à l'HDV, d'où qu'ils viennent : le MITM pour Dofus Retro, la capture tshark
//* pour Dofus 3. Les envois partent en tâche de fond dès la connexion au compte, la page HDV
//* sniffer n'est qu'un journal de ce qui a été reçu.
//*
//* Les prix sont rangés sous le jeu et le serveur sélectionnés dans le header (intercepteur de
//* clientV3Dofus). Seule garde : un prix Dofus 3 n'est jamais enregistré quand le sélecteur est
//* sur Retro, ni l'inverse.

export type PriceSource = 'retro' | 'dofus3';

export interface SnifferItemCapture {
  kind: 'item';
  id: string;
  source: PriceSource;
  versionId: number;
  itemId: number; // assetId
  instances: any[];
  averagePrice: number;
  timestamp: number;
}

export interface SnifferBatchCapture {
  kind: 'batch';
  id: string;
  source: PriceSource;
  versionId: number;
  count: number;
  pricedCount: number;
  //* Comptés au fil de l'envoi : enregistrés en base, ou absents de la base (non envoyables).
  savedCount: number;
  unknownCount: number;
  //* markRaw : plusieurs milliers d'entrées, jamais modifiées, affichées par pages.
  entries: { id: number; price: number }[];
  timestamp: number;
}

export type SnifferCapture = SnifferItemCapture | SnifferBatchCapture;

export interface ScanProgress {
  current: number;
  total: number;
  remaining: number;
}

const MAX_CAPTURES = 200;
const API_BATCH_SIZE = 500;

//* Avec plusieurs comptes connectés, la même liste de prix moyens arrive une fois par compte.
const DUPLICATE_LIST_WINDOW_MS = 60_000;

const metaKey = (versionId: number, assetId: number) => `${versionId}:${assetId}`;

//* Objets de chaque liste de prix encore en attente d'un verdict (enregistré ou inconnu), pour
//* compter ce qui a réellement été enregistré. Hors du state : jamais affiché tel quel.
const batchPending = new Map<string, Set<number>>();

const chunk = <T>(list: T[], size: number): T[][] => {
  const out: T[][] = [];
  for (let i = 0; i < list.length; i += size) out.push(list.slice(i, i + size));
  return out;
};

export const useHdvSnifferStore = defineStore('hdvSniffer', {
  state: () => ({
    captures: [] as SnifferCapture[],
    itemsMetadata: {} as Record<string, ItemLight>,
    failedAssetIds: new Set<string>(),
    //* versionId → (assetId → prix)
    pendingPrices: new Map<number, Map<number, number>>(),
    lastPriceList: null as { signature: string; at: number } | null,
    lastIgnored: null as { source: PriceSource; count: number; at: number } | null,
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

  getters: {
    metadataFor: (s) => (capture: SnifferItemCapture) =>
      s.itemsMetadata[metaKey(capture.versionId, capture.itemId)],

    metadataByAsset: (s) => (versionId: number, assetId: number) =>
      s.itemsMetadata[metaKey(versionId, assetId)],
  },

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

      //* MITM — Dofus Retro.
      window.electron.onProxyHdvPrices((data: any[]) => {
        if (!data || data.length === 0) return;
        this._onItemCaptured('retro', data[0].itemId, data, calculateAverageUnitPrice(data));
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

      //* Capture tshark — Dofus 3.
      window.electron.onCapturePriceList((payload: CapturePriceList) => this._onPriceList(payload));
      window.electron.onCaptureItemOffers((offer: CaptureItemOffers) => {
        const [p1, p10, p100] = offer.lots;
        const instances = [{ instanceId: `${offer.itemId}-${offer.receivedAt}`, p1, p10, p100 }];
        //* Le prix moyen reçu à la connexion prime ; à défaut, on le déduit des lots.
        const price = offer.knownAverage ?? calculateAverageUnitPrice(instances as any);
        this._onItemCaptured('dofus3', offer.itemId, instances, price);
      });

      this.isListenerActive = true;
    },

    //* Version sélectionnée si elle correspond à la source, null sinon.
    _versionFor(source: PriceSource): number | null {
      const dofus = useDofusStore();
      const code = dofus.currentGameVersion?.code;
      if (code !== source || dofus.currentGameServerId === null) return null;
      return dofus.currentGameVersionId;
    },

    _ignore(source: PriceSource, count: number) {
      this.lastIgnored = { source, count, at: Date.now() };
      console.warn(`[SnifferStore] ${count} prix ${source} ignorés : le sélecteur n'est pas sur ce jeu`);
    },

    _onItemCaptured(source: PriceSource, assetId: number, instances: any[], averagePrice: number) {
      const versionId = this._versionFor(source);
      if (versionId === null) return this._ignore(source, 1);

      this._pushCapture({
        kind: 'item',
        id: `${assetId}-${Date.now()}-${Math.random()}`,
        source,
        versionId,
        itemId: assetId,
        instances,
        averagePrice,
        timestamp: Date.now(),
      });

      if (averagePrice > 0) this._queue(versionId, [[assetId, averagePrice]]);
    },

    _onPriceList({ entries, receivedAt }: CapturePriceList) {
      const priced = entries.filter((e): e is { id: number; price: number } => e.price !== null && e.price > 0);

      const signature = `${priced.length}:${priced.reduce((acc, e) => (acc + e.id * 31 + e.price) % 1_000_000_007, 0)}`;
      if (this.lastPriceList?.signature === signature && receivedAt - this.lastPriceList.at < DUPLICATE_LIST_WINDOW_MS) {
        console.info('[SnifferStore] Liste de prix identique reçue il y a moins de 60 s, ignorée');
        return;
      }

      const versionId = this._versionFor('dofus3');
      if (versionId === null) return this._ignore('dofus3', priced.length);

      this.lastPriceList = { signature, at: receivedAt };

      this._pushCapture({
        kind: 'batch',
        id: `batch-${receivedAt}`,
        source: 'dofus3',
        versionId,
        count: entries.length,
        pricedCount: priced.length,
        savedCount: 0,
        unknownCount: 0,
        entries: markRaw(priced),
        timestamp: receivedAt,
      });

      batchPending.set(`batch-${receivedAt}`, new Set(priced.map(e => e.id)));
      this._queue(versionId, priced.map(e => [e.id, e.price]));
      this._debounceSync(true);
    },

    _pushCapture(capture: SnifferCapture) {
      this.captures.unshift(capture);
      if (this.captures.length > MAX_CAPTURES) this.captures.pop();
    },

    //* Reporte le verdict d'un objet sur les listes de prix qui l'attendaient encore.
    _settleBatches(assetIds: number[], outcome: 'saved' | 'unknown') {
      if (batchPending.size === 0) return;
      for (const capture of this.captures) {
        if (capture.kind !== 'batch') continue;
        const waiting = batchPending.get(capture.id);
        if (!waiting) continue;
        for (const id of assetIds) {
          if (!waiting.delete(id)) continue;
          if (outcome === 'saved') capture.savedCount++;
          else capture.unknownCount++;
        }
        if (waiting.size === 0) batchPending.delete(capture.id);
      }
    },

    _queue(versionId: number, prices: [number, number][]) {
      let pending = this.pendingPrices.get(versionId);
      if (!pending) {
        pending = new Map();
        this.pendingPrices.set(versionId, pending);
      }
      const alreadyUnknown: number[] = [];
      for (const [assetId, price] of prices) {
        if (this.failedAssetIds.has(metaKey(versionId, assetId))) alreadyUnknown.push(assetId);
        else pending.set(assetId, price);
      }
      this._settleBatches(alreadyUnknown, 'unknown');
      this._debounceSync();
    },

    _debounceSync(force = false) {
      if (this.fetchTimeout) clearTimeout(this.fetchTimeout);
      this.fetchTimeout = setTimeout(() => this.syncAll(), force ? 500 : 3000);
    },

    //* N'envoie que les prix du jeu actuellement sélectionné : l'intercepteur range la requête
    //* sous ce jeu. Ceux d'un autre jeu attendent qu'on y revienne.
    async syncAll() {
      if (this.isSyncing) return;

      const versionId = useDofusStore().currentGameVersionId;
      const pending = versionId !== null ? this.pendingPrices.get(versionId) : undefined;
      if (!pending || pending.size === 0) return;

      this.isSyncing = true;

      try {
        const prices = Array.from(pending.entries());

        const missingIds = prices
          .map(([assetId]) => assetId)
          .filter(id => !this.itemsMetadata[metaKey(versionId!, id)] && !this.failedAssetIds.has(metaKey(versionId!, id)));

        for (const ids of chunk(missingIds, API_BATCH_SIZE)) {
          try {
            const { data } = await useFetchItemsByAssetIds(ids);
            const foundIds = new Set(data.map(item => item.assetId));
            data.forEach(item => this.itemsMetadata[metaKey(versionId!, item.assetId)] = markRaw(item));

            const unknown = ids.filter(id => !foundIds.has(id));
            unknown.forEach(id => {
                this.failedAssetIds.add(metaKey(versionId!, id));
                pending.delete(id);
            });
            this._settleBatches(unknown, 'unknown');
          } catch (e) {
            console.error('[SnifferStore] Metadata fetch error:', e);
          }
        }

        const priceBatch = prices
          .map(([assetId, price]) => {
            const meta = this.itemsMetadata[metaKey(versionId!, assetId)];
            return meta ? { assetId, itemId: meta.id, price } : null;
          })
          .filter((p): p is { assetId: number; itemId: number; price: number } => p !== null);

        for (const batch of chunk(priceBatch, API_BATCH_SIZE)) {
          await useMutationItemPrices(batch.map(({ itemId, price }) => ({ itemId, price })), { source: 'capture' });
          batch.forEach(p => pending.delete(p.assetId));
          this._settleBatches(batch.map(p => p.assetId), 'saved');
        }

        //* Seuls les prix déjà affichés quelque part sont rafraîchis : relire les 7 600 objets de
        //* la liste des prix moyens ne servirait à rien.
        const itemPrices = useItemPrices();
        const displayed = priceBatch.map(p => p.itemId).filter(id => itemPrices.get(id));
        if (displayed.length > 0) await itemPrices.refreshRecursive(displayed);
      } catch (e) {
        console.error('[SnifferStore] Sync Error:', e);
      } finally {
        this.isSyncing = false;
        if (pending.size > 0) this._debounceSync();
      }
    },

    clear() {
      this.captures = [];
      this.pendingPrices.clear();
      this.scanProgress = null;
    },

    clearDisplay() {
        this.captures = [];
        this.lastIgnored = null;
    }
  }
});
