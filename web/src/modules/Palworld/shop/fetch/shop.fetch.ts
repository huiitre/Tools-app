import { clientV3 } from '@/services/axiosInstance'
import type { Merchant } from '../types/shop.types'

export async function fetchMerchants(): Promise<Merchant[]> {
  const { data } = await clientV3.get<Merchant[]>('/palworld/shop/merchants')
  return data
}
