import type { IUpdateService } from "@/services/update/IUpdateService"

export class ElectronUpdateService implements IUpdateService {
  onUpdateAvailable(callback: () => void): void {
    window.electron?.onUpdateAvailable(callback)
  }

  applyUpdate(): void {
    window.electron?.applyUpdate()
  }
}