export type AdminVpnColumn = {
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

export const ADMIN_VPN_PAGE_SIZES = [10, 20, 50, 100] as const
export type AdminVpnPageSize = (typeof ADMIN_VPN_PAGE_SIZES)[number]

// Contrôles d'intégrité renvoyés par le service WireGuard : « cette conf peut-elle encore
// fonctionner ? », question distincte de `status` qui ne parle que de connectivité.
export type VpnPeerChecks = {
  keyPair: boolean
  serverKey: boolean
  onInterface: boolean
  inConfig: boolean
}

// `orphan` = présent sur disque mais absent de l'interface. `never` ne veut pas dire « jamais
// connecté » mais « aucune poignée de main depuis le démarrage de l'interface ».
export type VpnPeerStatus = 'connected' | 'idle' | 'never' | 'orphan'

export type VpnPeer = {
  name: string
  ip: string
  publicKey: string
  status: VpnPeerStatus
  handshakeSecondsAgo: number | null
  rxBytes: number
  txBytes: number
  valid: boolean
  checks: VpnPeerChecks
}

// Contrainte du script wg-users.sh : la refuser ici évite un aller-retour pour rien.
export const VPN_PEER_NAME_PATTERN = /^[A-Za-z0-9][A-Za-z0-9_-]{0,30}$/
