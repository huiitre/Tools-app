//* Peers WireGuard servis par l'API Core.
//* Le téléchargement de la configuration attend encore son use case côté API.
import { clientCore } from '@/services/axiosInstance'
import type { VpnPeer } from '../types/adminVpn.types'

export async function fetchVpnPeers(): Promise<VpnPeer[]> {
  const { data } = await clientCore.get<VpnPeer[]>('/vpn/peers')
  return data
}

export async function createVpnPeer(name: string): Promise<void> {
  await clientCore.post('/vpn/peers', { name })
}

export function downloadVpnPeerConfig(name: string) {
  return clientCore.get(`/vpn/peers/${encodeURIComponent(name)}/config`, { responseType: 'blob' })
}

export async function deleteVpnPeer(name: string): Promise<void> {
  await clientCore.delete(`/vpn/peers/${encodeURIComponent(name)}`)
}
