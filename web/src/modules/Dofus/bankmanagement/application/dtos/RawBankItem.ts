/** Format brut reçu depuis Electron via le sniffer */
export interface RawBankItem {
  instanceId: string;
  assetId: number;
  quantity: number;
  position: string | null;
  stats: string;
}
