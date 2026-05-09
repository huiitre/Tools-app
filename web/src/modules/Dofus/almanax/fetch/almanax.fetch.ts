import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchAlmanax = async () => {
  return await clientV3Dofus.get('/dofus/almanax')
}
