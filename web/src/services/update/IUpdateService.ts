export interface IUpdateService {
  onUpdateReady(callback: () => void): void
  applyUpdate(): void
}