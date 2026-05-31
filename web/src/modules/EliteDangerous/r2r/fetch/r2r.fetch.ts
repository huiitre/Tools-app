import { clientV3 } from '@/services/axiosInstance'
import type { R2rExpeditionDetail, R2rExpeditionSummary } from '../types/r2r.types'

const BASE = '/elite-dangerous/r2r/expeditions'

export const fetchExpeditions = () =>
  clientV3.get<R2rExpeditionSummary[]>(BASE)

export const fetchExpedition = (id: string) =>
  clientV3.get<R2rExpeditionDetail>(`${BASE}/${id}`)

export const importExpedition = (formData: FormData) =>
  clientV3.post<string>(BASE, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })

export const updateProgress = (id: string, currentSystemIndex: number, currentBodiesDone: number[]) =>
  clientV3.patch(`${BASE}/${id}/progress`, { currentSystemIndex, currentBodiesDone })

export const renameExpedition = (id: string, name: string) =>
  clientV3.patch(`${BASE}/${id}/name`, { name })

export const exportExpedition = (id: string) =>
  clientV3.get(`${BASE}/${id}/export`, { responseType: 'blob' })

export const deleteExpedition = (id: string) =>
  clientV3.delete(`${BASE}/${id}`)