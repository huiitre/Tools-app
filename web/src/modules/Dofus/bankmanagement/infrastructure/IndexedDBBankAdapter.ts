import type { BankItem } from '../domain/BankItem';
import type { IBankRepository } from '../domain/IBankRepository';
import { indexedDBService } from '@/services/IndexedDBService';

export class IndexedDBBankAdapter implements IBankRepository {
  private readonly storeName = 'bank_items';
  private readonly metaStoreName = 'bank_metadata';

  async getByServer(versionId: number, serverId: number): Promise<BankItem[]> {
    const store = await indexedDBService.getStore(this.storeName);
    const index = store.index('serverPartition');
    const range = IDBKeyRange.only([versionId, serverId]);
    
    return new Promise((resolve, reject) => {
      const request = index.getAll(range);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  async saveAll(items: BankItem[]): Promise<void> {
    const db = await indexedDBService.init();
    const transaction = db.transaction(this.storeName, 'readwrite');
    const store = transaction.objectStore(this.storeName);

    return new Promise((resolve, reject) => {
      items.forEach(item => store.put(item));
      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }

  async deleteMany(instanceIds: string[], versionId: number, serverId: number): Promise<void> {
    const db = await indexedDBService.init();
    const transaction = db.transaction(this.storeName, 'readwrite');
    const store = transaction.objectStore(this.storeName);

    return new Promise((resolve, reject) => {
      instanceIds.forEach(id => store.delete([versionId, serverId, id]));
      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }

  async clearServer(versionId: number, serverId: number): Promise<void> {
    const db = await indexedDBService.init();
    const transaction = db.transaction([this.storeName, this.metaStoreName], 'readwrite');
    const store = transaction.objectStore(this.storeName);
    const metaStore = transaction.objectStore(this.metaStoreName);
    const index = store.index('serverPartition');
    const range = IDBKeyRange.only([versionId, serverId]);

    return new Promise((resolve, reject) => {
      // 1. Supprimer les items
      const request = index.openKeyCursor(range);
      request.onsuccess = (event: any) => {
        const cursor = event.target.result;
        if (cursor) {
          store.delete(cursor.primaryKey);
          cursor.continue();
        }
      };

      // 2. Supprimer la meta
      metaStore.delete([versionId, serverId]);

      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }

  /* --- METADATA --- */

  async getLastSync(versionId: number, serverId: number): Promise<number | null> {
    const store = await indexedDBService.getStore(this.metaStoreName);
    return new Promise((resolve, reject) => {
      const request = store.get([versionId, serverId]);
      request.onsuccess = () => resolve(request.result?.lastSync || null);
      request.onerror = () => reject(request.error);
    });
  }

  async saveLastSync(versionId: number, serverId: number, timestamp: number): Promise<void> {
    const db = await indexedDBService.init();
    const transaction = db.transaction(this.metaStoreName, 'readwrite');
    const store = transaction.objectStore(this.metaStoreName);

    return new Promise((resolve, reject) => {
      store.put({ gameVersionId: versionId, serverId: serverId, lastSync: timestamp });
      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
    });
  }
}
