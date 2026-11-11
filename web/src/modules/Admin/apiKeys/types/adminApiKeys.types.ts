// Miroir de ApiKeyView : ni la clé en clair ni son hash, seul le préfixe permet de la reconnaître.
export type ApiKey = {
  id: number
  name: string
  description: string
  keyPrefix: string
  scopes: string[]
  // Scopes stockés que le catalogue ne connaît plus : sans effet, affichés pour être nettoyés.
  unknownScopes: string[]
  createdAt: string
  createdBy: number
  expiresAt: string
  revokedAt: string | null
  lastUsedAt: string | null
  lastUsedIp: string | null
  isExpired: boolean
  isRevoked: boolean
}

// Seule réponse qui contienne la clé en clair.
export type CreatedApiKey = {
  apiKey: ApiKey
  key: string
}

export type CreateApiKeyPayload = {
  name: string
  description: string
  expiresAt: string
  scopes: string[]
}

export type AdminApiKeyColumn = {
  key: string
  label: string
  description: string
  visible: boolean
  sortable: boolean
  userToggle: boolean
  minSize: number
  maxSize: number
  grow: number
}

export type AdminSortDir = 'ASC' | 'DESC'

export type ApiKeyStatusFilter = 'active' | 'revoked' | 'all'

export const ADMIN_API_KEY_PAGE_SIZES = [10, 20, 50, 100] as const
export type AdminApiKeyPageSize = (typeof ADMIN_API_KEY_PAGE_SIZES)[number]

// Mêmes bornes que l'API (ApiKey.cs) : les refuser ici évite un aller-retour pour rien.
export const API_KEY_NAME_MAX_LENGTH = 100
export const API_KEY_DESCRIPTION_MAX_LENGTH = 500
export const API_KEY_MAX_LIFETIME_YEARS = 3
