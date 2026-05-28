import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchAlmanax = async () => {
  return await clientV3Dofus.get('/dofus/almanax')
}

export const fetchAlmanaxSubscriptions = async (): Promise<number[]> => {
  const { data } = await clientV3Dofus.get('/dofus/almanax/subscriptions')
  return data
}

export const addAlmanaxSubscription = async (almanaxId: number, date: string): Promise<void> => {
  await clientV3Dofus.post('/dofus/almanax/subscriptions', { almanaxId, date })
}

export const removeAlmanaxSubscription = async (almanaxId: number): Promise<void> => {
  await clientV3Dofus.delete(`/dofus/almanax/subscriptions/${almanaxId}`)
}
