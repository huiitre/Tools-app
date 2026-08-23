//* Peers WireGuard servis par l'API Core.
//* Seul GET /vpn existe côté API : la création et la suppression attendent leur use case.
import { clientCore } from '@/services/axiosInstance'
import type { VpnPeer } from '../types/adminVpn.types'

export async function fetchVpnPeers(): Promise<VpnPeer[]> {
  const { data } = await clientCore.get<VpnPeer[]>('/vpn')
  return data
}

export async function createVpnPeer(name: string): Promise<void> {
  await clientCore.post('/vpn', { name })
}

export function downloadVpnPeerConfig(name: string) {
  return clientCore.get(`/vpn/${encodeURIComponent(name)}/config`, { responseType: 'blob' })
}

export async function deleteVpnPeer(name: string): Promise<void> {
  await clientCore.delete(`/vpn/${encodeURIComponent(name)}`)
}
