import { Item } from "@/modules/Dofus/item/types/item.types"

export type Almanax = {
  id: number
  name: string
  description: string
  date: string
  item: Item | null,
  quantity: number
}