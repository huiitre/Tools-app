import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchWorkshopBreeds = async () => {
  return await clientV3Dofus.get('/dofus/workshop/breeds')
}

export const useAddBreedsToWorkshop = async (workshopId: number, breedIds: number[]) => {
  return await clientV3Dofus.post(`/dofus/workshops/${workshopId}/breeds`, breedIds)
}

export const useRemoveBreedFromWorkshop = async (workshopId: number, breedId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/breeds/${breedId}`)
}
