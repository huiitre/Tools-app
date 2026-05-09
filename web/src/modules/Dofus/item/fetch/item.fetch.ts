import { clientV3Dofus } from "@/services/axiosInstance"
import { ItemLight, ItemType } from "../types/item.types"

export const useFetchItemPrices = async (itemIds: number[]) => {
  return clientV3Dofus.post("/dofus/item/prices", { itemIds })
}

export const useMutationItemPrices = async(items: { itemId: number; price: number }[]) => {
  return clientV3Dofus.put("/dofus/item/prices/batch", items)
}

export const useFetchItemsByAssetIds = async (assetIds: number[]) => {
  return clientV3Dofus.get<ItemLight[]>("/dofus/item/metadata/batch", {
    params: { assetIds: assetIds.join(',') }
  })
}

export const useFetchItemTypes = async () => {
  return clientV3Dofus.get<ItemType[]>("/dofus/item-types")
}
