//* Apps Android distribuées par huihuistore, servies par l'API Core.
import { clientCore } from '@/services/axiosInstance'
import type {
  AndroidApp,
  AndroidAppImportAnalysis,
  ConfirmAndroidAppImportPayload,
  UpdateAndroidAppPayload,
} from '../types/adminAndroidApps.types'

const BASE = '/admin/android-apps'

export async function fetchAndroidApps(): Promise<AndroidApp[]> {
  const { data } = await clientCore.get<AndroidApp[]>(BASE)
  return data
}

// Premier temps de l'import : l'APK part une seule fois, la suite ne renvoie que son importId.
export async function analyzeAndroidAppImport(
  file: File,
  onProgress?: (ratio: number) => void,
): Promise<AndroidAppImportAnalysis> {
  const formData = new FormData()
  formData.append('File', file)

  const { data } = await clientCore.post<AndroidAppImportAnalysis>(`${BASE}/imports`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    // Un APK de 100 Mo prend du temps à monter et l'analyse lance deux outils : pas de timeout.
    timeout: 0,
    onUploadProgress: event => {
      if (onProgress && event.total) onProgress(event.loaded / event.total)
    },
  })
  return data
}

export async function confirmAndroidAppImport(
  importId: string,
  payload: ConfirmAndroidAppImportPayload,
): Promise<AndroidApp> {
  const { data } = await clientCore.post<AndroidApp>(`${BASE}/imports/${importId}/confirm`, payload, {
    timeout: 0,
  })
  return data
}

export async function cancelAndroidAppImport(importId: string): Promise<void> {
  await clientCore.delete(`${BASE}/imports/${importId}`)
}

export async function updateAndroidApp(appId: number, payload: UpdateAndroidAppPayload): Promise<AndroidApp> {
  const { data } = await clientCore.patch<AndroidApp>(`${BASE}/${appId}`, payload)
  return data
}

export async function deleteAndroidAppRelease(appId: number, releaseId: number): Promise<void> {
  await clientCore.delete(`${BASE}/${appId}/releases/${releaseId}`)
}

// Supprime aussi tous les fichiers APK de l'app.
export async function deleteAndroidApp(appId: number): Promise<void> {
  await clientCore.delete(`${BASE}/${appId}`)
}
