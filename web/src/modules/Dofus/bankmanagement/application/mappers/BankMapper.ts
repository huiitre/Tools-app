import type { BankItem } from '../../domain/BankItem';
import type { RawBankItem } from '../dtos/RawBankItem';
import type { ItemLight } from '@/modules/Dofus/item/types/item.types';

export class BankMapper {
  /** 
   * Transforme un item brut du sniffer en entité de domaine partielle (sans metadata) 
   */
  static fromRaw(raw: RawBankItem, versionId: number, serverId: number): BankItem {
    return {
      instanceId: raw.instanceId,
      assetId: raw.assetId,
      quantity: raw.quantity,
      stats: raw.stats,
      gameVersionId: versionId,
      serverId: serverId
    };
  }

  /**
   * Fusionne les métadonnées de l'API avec une entité existante
   */
  static mergeMetadata(item: BankItem, metadata: ItemLight): BankItem {
    return {
      ...item,
      metadata: metadata
    };
  }
}
