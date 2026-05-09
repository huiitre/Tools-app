import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchWorkshopTags = async () => {
  return await clientV3Dofus.get('/dofus/workshop/tags')
}

export const useCreateWorkshopTag = async (data: { name: string; color: string }) => {
  return await clientV3Dofus.post('/dofus/workshop/tags', data)
}

export const useUpdateWorkshopTag = async (tagId: number, data: { name: string; color: string }) => {
  return await clientV3Dofus.patch(`/dofus/workshop/tags/${tagId}`, data)
}

export const useDeleteWorkshopTag = async (tagId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshop/tags/${tagId}`)
}

export const useAddTagsToWorkshop = async (workshopId: number, tagIds: number[]) => {
  return await clientV3Dofus.post(`/dofus/workshops/${workshopId}/tags`, tagIds)
}

export const useRemoveTagFromWorkshop = async (workshopId: number, tagId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/tags/${tagId}`)
}