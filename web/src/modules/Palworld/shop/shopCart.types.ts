export interface ShopCartLine {
  merchantId: string
  itemSlug: string
  quantity: number
}

export interface ShopCartState {
  current: ShopCartLine[]
  presets: Record<string, ShopCartLine[]>
}
