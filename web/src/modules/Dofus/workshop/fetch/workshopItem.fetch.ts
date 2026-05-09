import { Item } from '@/modules/Dofus/item/types/item.types'
import { WorkshopItem, WorkshopItemIngredient } from '@/modules/Dofus/workshop/types/workshop.types'
import { clientV3Dofus } from '@/services/axiosInstance'

export const useSearchCraftableItems = async (workshopId: number, query?: string): Promise<Item[]> => {
  const response = await clientV3Dofus.get(`/dofus/workshops/${workshopId}/items/search`, {
    params: { query }
  })
  return response.data
}

export const useAddItemsToWorkshop = async (workshopId: number, itemIds: number[]): Promise<WorkshopItem[]> => {
  const response = await clientV3Dofus.post(`/dofus/workshops/${workshopId}/items`, itemIds)
  return response.data
}

export const useDeleteWorkshopItem = async (workshopId: number, workshopItemId: number): Promise<void> => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/items/${workshopItemId}`)
}

export const useUpdateWorkshopItemQuantity = async (workshopId: number, workshopItemId: number, quantity: number): Promise<void> => {
  return await clientV3Dofus.patch(`/dofus/workshops/${workshopId}/items/${workshopItemId}/quantity/${quantity}`)
}

export const useCraftIngredient = async (workshopId: number, workshopItemId: number, ingredientId: number): Promise<WorkshopItemIngredient[]> => {
  const response = await clientV3Dofus.post(`/dofus/workshops/${workshopId}/items/${workshopItemId}/ingredients/${ingredientId}/craft`)
  return response.data
}

export const useUncraftIngredient = async (workshopId: number, ingredientId: number): Promise<void> => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/ingredients/${ingredientId}/crafted`)
}

export const useUpdateIngredientQuantityObtained = async (workshopId: number, ingredientId: number, quantityObtained: number): Promise<void> => {
  return await clientV3Dofus.patch(`/dofus/workshops/${workshopId}/ingredients/${ingredientId}/obtained/${quantityObtained}`)
}