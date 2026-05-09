export class IndexedDBService {
  private db: IDBDatabase | null = null;
  private readonly dbName = 'ToolsWebDB';
  private readonly version = 4; // Passage en v4 pour autofocus_mapping

  async init(): Promise<IDBDatabase> {
    if (this.db) return this.db;

    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.version);

      request.onupgradeneeded = (event: any) => {
        const db = event.target.result;
        
        // Store pour la banque (clé composite)
        if (!db.objectStoreNames.contains('bank_items')) {
          const bankStore = db.createObjectStore('bank_items', { 
            keyPath: ['gameVersionId', 'serverId', 'instanceId'] 
          });
          bankStore.createIndex('serverPartition', ['gameVersionId', 'serverId'], { unique: false });
        }

        // Store pour les métadonnées de la banque (dernière synchro, etc.)
        if (!db.objectStoreNames.contains('bank_metadata')) {
          db.createObjectStore('bank_metadata', { keyPath: ['gameVersionId', 'serverId'] });
        }

        // Store pour l'autofocus mapping
        if (!db.objectStoreNames.contains('autofocus_mapping')) {
          db.createObjectStore('autofocus_mapping', { keyPath: 'id' });
        }
      };

      request.onsuccess = () => {
        this.db = request.result;
        resolve(this.db);
      };

      request.onerror = () => reject(request.error);
    });
  }

  async getStore(storeName: string, mode: IDBTransactionMode = 'readonly'): Promise<IDBObjectStore> {
    const db = await this.init();
    return db.transaction(storeName, mode).objectStore(storeName);
  }
}

export const indexedDBService = new IndexedDBService();
