import type { ItemLight } from '@/modules/Dofus/item/types/item.types';

export interface BankItem {
  /** Identifiant unique de l'instance de l'objet (clé primaire) */
  instanceId: string;
  /** ID du jeu (pour fetch metadata) */
  assetId: number;
  /** Quantité en banque */
  quantity: number;
  /** Jet de l'item (raw stats) */
  stats: string;
  /** Métadonnées complètes de l'item */
  metadata?: ItemLight;
  
  // Partitionnement
  gameVersionId: number;
  serverId: number;
}
