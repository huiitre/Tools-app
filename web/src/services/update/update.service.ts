import type { IUpdateService } from '@/services/update/IUpdateService'
import { WebUpdateService } from '@/services/update/WebUpdateService'
import { ElectronUpdateService } from '@/services/update/ElectronUpdateService'
import { useEnv } from '@/composables/useEnv'

function createUpdateService(): IUpdateService {
  const { isElectron } = useEnv()
  if (isElectron) {
    return new ElectronUpdateService()
  }
  return new WebUpdateService()
}

export const updateService: IUpdateService = createUpdateService()
