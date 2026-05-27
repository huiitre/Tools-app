export interface IUpdateService {
  onUpdateAvailable(callback: () => void): void
  startDownload(): void
  onDownloadProgress(callback: (percent: number) => void): void
  onUpdateReady(callback: () => void): void
  applyUpdate(): void
}