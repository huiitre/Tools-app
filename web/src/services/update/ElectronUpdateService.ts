import type { IUpdateService } from "@/services/update/IUpdateService"

export class ElectronUpdateService implements IUpdateService {
  onUpdateAvailable(callback: () => void): void {
    window.electron?.onUpdateAvailable(callback)
  }

  startDownload(): void {
    window.electron?.startDownload()
  }

  onDownloadProgress(callback: (percent: number) => void): void {
    window.electron?.onDownloadProgress(callback)
  }

  onUpdateReady(callback: () => void): void {
    window.electron?.onUpdateDownloaded(callback)
  }

  applyUpdate(): void {
    window.electron?.applyUpdate()
  }
}