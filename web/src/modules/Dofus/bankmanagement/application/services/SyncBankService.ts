import type { IBankRepository } from '../../domain/IBankRepository';
import type { RawBankItem } from '../dtos/RawBankItem';
import { BankMapper } from '../mappers/BankMapper';
import { useFetchItemsByAssetIds } from '@/modules/Dofus/item/fetch/item.fetch';
import type { BankItem } from '../../domain/BankItem';

export class SyncBankService {
  constructor(private readonly repository: IBankRepository) {}

  /**
   * Orchestre la synchronisation.
   * @param isFullDump Si true, on supprime les items qui ne sont plus dans le scan.
   */
  async sync(scan: RawBankItem[], versionId: number, serverId: number, isFullDump: boolean = false): Promise<void> {
    const existingItems = await this.repository.getByServer(versionId, serverId);
    const existingMap = new Map<string, BankItem>(existingItems.map(i => [i.instanceId, i]));
    const scanMap = new Map<string, RawBankItem>(scan.map(r => [r.instanceId, r]));

    // 1. Identifier les suppressions (UNIQUEMENT si c'est un Full Dump)
    let instancesToDelete: string[] = [];
    if (isFullDump) {
      instancesToDelete = existingItems
        .filter(i => !scanMap.has(i.instanceId))
        .map(i => i.instanceId);
    }

    // 2. Identifier les nouveaux items et ceux à mettre à jour
    const itemsToSave: BankItem[] = [];
    const assetIdsToFetch = new Set<number>();

    for (const raw of scan) {
      const existing = existingMap.get(raw.instanceId);

      if (!existing) {
        itemsToSave.push(BankMapper.fromRaw(raw, versionId, serverId));
        assetIdsToFetch.add(raw.assetId);
      } else if (existing.quantity !== raw.quantity || existing.stats !== raw.stats) {
        const updated = BankMapper.fromRaw(raw, versionId, serverId);
        itemsToSave.push({ ...updated, metadata: existing.metadata });
      }
    }

    // 3. Batch fetch metadata
    const validAssetIds = Array.from(assetIdsToFetch).filter(id => !isNaN(id));
    if (validAssetIds.length > 0) {
      try {
        const { data: metadataList } = await useFetchItemsByAssetIds(validAssetIds);
        const metaMap = new Map(metadataList.map(m => [m.assetId, m]));

        itemsToSave.forEach(item => {
          if (!item.metadata) {
            const meta = metaMap.get(item.assetId);
            if (meta) item.metadata = meta;
          }
        });
      } catch (error) {
        console.error('[SyncBankService] Metadata fetch error:', error);
      }
    }

    // 4. Appliquer les changements
    if (instancesToDelete.length > 0) {
      await this.repository.deleteMany(instancesToDelete, versionId, serverId);
    }
    
    if (itemsToSave.length > 0) {
      await this.repository.saveAll(itemsToSave);
    }

    console.log(`[SyncBankService] Synchro terminée (Full Dump: ${isFullDump}). ${itemsToSave.length} modifiés, ${instancesToDelete.length} supprimés.`);
  }
}
