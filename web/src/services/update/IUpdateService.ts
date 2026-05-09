export interface IUpdateService {
  onUpdateAvailable(callback: () => void): void
  applyUpdate(): void
}