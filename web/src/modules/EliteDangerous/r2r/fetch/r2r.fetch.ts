import { clientCore } from '@/services/axiosInstance'
import type { R2rExpeditionDetail, R2rExpeditionSummary } from '../types/r2r.types'

const BASE = '/elite-dangerous/expeditions'

export const fetchExpeditions = () =>
  clientCore.get<R2rExpeditionSummary[]>(BASE)

export const fetchExpedition = (id: string) =>
  clientCore.get<R2rExpeditionDetail>(`${BASE}/${id}`)

export const importExpedition = (formData: FormData) =>
  clientCore.post<{ id: string }>(BASE, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })

export const updateProgress = (id: string, currentSystemIndex: number, currentBodiesDone: number[]) =>
  clientCore.patch(`${BASE}/${id}/progress`, { currentSystemIndex, currentBodiesDone })

export const renameExpedition = (id: string, name: string) =>
  clientCore.patch(`${BASE}/${id}/name`, { name })

export const exportExpedition = (id: string) =>
  clientCore.get(`${BASE}/${id}/export`, { responseType: 'blob' })

export const deleteExpedition = (id: string) =>
  clientCore.delete(`${BASE}/${id}`)