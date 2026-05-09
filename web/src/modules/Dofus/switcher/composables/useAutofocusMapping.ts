import { ref } from 'vue';
import { indexedDBService } from '@/services/IndexedDBService';

export interface AutofocusMapping {
    id: string; // ID Hex (ex: e082d4ba0d)
    name: string; // Nom du personnage
}

const mapping = ref<Record<string, string>>({});

export function useAutofocusMapping() {
    async function loadMapping() {
        const log = (msg: string, data?: any) => {
            window.switcher?.log({ level: 'info', service: 'useAutofocusMapping', message: msg, data });
        };

        try {
            log('Tentative de lecture du mapping depuis IndexedDB');
            const store = await indexedDBService.getStore('autofocus_mapping', 'readonly');
            return new Promise<void>((resolve, reject) => {
                const request = store.getAll();
                request.onsuccess = () => {
                    const results = request.result as AutofocusMapping[];
                    const map: Record<string, string> = {};
                    results.forEach(item => {
                        map[item.id] = item.name;
                    });
                    mapping.value = map;
                    log(`Mapping chargé : ${Object.keys(map).length} entrées`);
                    // Sync with electron
                    window.electron?.setAutofocusMapping(map);
                    window.switcher?.setAutofocusMapping(map);
                    resolve();
                };
                request.onerror = () => {
                    log('Erreur lors de getAll()', request.error);
                    reject(request.error);
                };
            });
        } catch (err: any) {
            log('Erreur getStore', err.message);
            throw err;
        }
    }

    async function saveEntry(id: string, name: string) {
        const store = await indexedDBService.getStore('autofocus_mapping', 'readwrite');
        return new Promise<void>((resolve, reject) => {
            const request = store.put({ id, name });
            request.onsuccess = () => {
                mapping.value[id] = name;
                // On passe une copie simple (POJO) pour éviter DataCloneError avec le Proxy Vue
                window.electron?.setAutofocusMapping({ ...mapping.value });
                resolve();
            };
            request.onerror = () => reject(request.error);
        });
    }

    async function deleteEntry(id: string) {
        const store = await indexedDBService.getStore('autofocus_mapping', 'readwrite');
        return new Promise<void>((resolve, reject) => {
            const request = store.delete(id);
            request.onsuccess = () => {
                delete mapping.value[id];
                // On passe une copie simple (POJO) pour éviter DataCloneError avec le Proxy Vue
                window.electron?.setAutofocusMapping({ ...mapping.value });
                resolve();
            };
            request.onerror = () => reject(request.error);
        });
    }

    async function clearMapping() {
        const store = await indexedDBService.getStore('autofocus_mapping', 'readwrite');
        return new Promise<void>((resolve, reject) => {
            const request = store.clear();
            request.onsuccess = () => {
                mapping.value = {};
                window.electron?.setAutofocusMapping({});
                window.switcher?.setAutofocusMapping?.({}); // Cas où on est dans le switcher
                resolve();
            };
            request.onerror = () => reject(request.error);
        });
    }

    // Listener pour les mises à jour automatiques depuis le sniffer
    const api = window.electron || window.switcher;
    if (api && 'onAutofocusMappingUpdated' in api) {
        (api as any).onAutofocusMappingUpdated(async (newMapping: Record<string, string>) => {
            for (const [id, name] of Object.entries(newMapping)) {
                if (mapping.value[id] !== name) {
                    await saveEntry(id, name);
                }
            }
        });
    }

    return {
        mapping,
        loadMapping,
        saveEntry,
        deleteEntry,
        clearMapping
    };
}
