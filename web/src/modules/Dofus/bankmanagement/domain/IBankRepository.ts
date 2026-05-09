import type { BankItem } from './BankItem';

export interface IBankRepository {
  getByServer(versionId: number, serverId: number): Promise<BankItem[]>;
  saveAll(items: BankItem[]): Promise<void>;
  deleteMany(instanceIds: string[], versionId: number, serverId: number): Promise<void>;
  clearServer(versionId: number, serverId: number): Promise<void>;
}
