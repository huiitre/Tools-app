export interface IUpdateService {
  onUpdateAvailable(callback: () => void): void
  onDownloadProgress(callback: (percent: number) => void): void
  onUpdateReady(callback: () => void): void
  startDownload(): void
  applyUpdate(): void
}
