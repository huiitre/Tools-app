export interface ShopCurrency {
  itemId: string
  name: string | null
  iconUrl: string | null
}

export interface MerchantOffer {
  itemId: number
  itemSlug: string
  itemName: string
  itemIconUrl: string | null
  itemMaxStackCount: number | null
  price: number
  quantityPerPurchase: number
  productType: 'NORMAL' | 'ONLY_PURCHASE_ONE'
}

export interface Merchant {
  id: number
  externalId: string
  code: string
  name: string | null
  portraitUrl: string | null
  restockMinute: number | null
  currency: ShopCurrency
  offers: MerchantOffer[]
}
