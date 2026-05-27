import type { IUpdateService } from "@/services/update/IUpdateService"

export class ElectronUpdateService implements IUpdateService {
  onUpdateReady(callback: () => void): void {
    window.electron?.onUpdateReady(callback)
  }

  applyUpdate(): void {
    window.electron?.applyUpdate()
  }
}