import { clientV3 } from '@/services/axiosInstance'
import type { RiotRegion, ValorantAccount } from '@/modules/Riot/riot.store'

export async function listAccounts(): Promise<ValorantAccount[]> {
  const { data } = await clientV3.get<ValorantAccount[]>('/riot/valorant/accounts')
  return data
}

export async function linkAccount(
  refreshToken: string,
  region: RiotRegion,
  label?: string,
): Promise<{ account: ValorantAccount; accessToken: string }> {
  const { data } = await clientV3.post('/riot/valorant/accounts', { refreshToken, region, label })
  return data
}

export async function unlinkAccount(accountId: number): Promise<void> {
  await clientV3.delete(`/riot/valorant/accounts/${accountId}`)
}

export async function renameAccount(accountId: number, label: string): Promise<ValorantAccount> {
  const { data } = await clientV3.put<ValorantAccount>(`/riot/valorant/accounts/${accountId}`, { label })
  return data
}
