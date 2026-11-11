//* Clés d'API des services du NAS, servies par l'API Core.
import { clientCore } from '@/services/axiosInstance'
import type { ApiKey, CreatedApiKey, CreateApiKeyPayload } from '../types/adminApiKeys.types'

export async function fetchApiKeys(): Promise<ApiKey[]> {
  const { data } = await clientCore.get<ApiKey[]>('/admin/api-keys')
  return data
}

export async function fetchApiKeyScopes(): Promise<string[]> {
  const { data } = await clientCore.get<string[]>('/admin/api-keys/scopes')
  return data
}

export async function createApiKey(payload: CreateApiKeyPayload): Promise<CreatedApiKey> {
  const { data } = await clientCore.post<CreatedApiKey>('/admin/api-keys', payload)
  return data
}

export async function revokeApiKey(id: number): Promise<void> {
  await clientCore.post(`/admin/api-keys/${id}/revoke`)
}
