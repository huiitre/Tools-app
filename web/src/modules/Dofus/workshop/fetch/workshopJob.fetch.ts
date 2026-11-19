import { clientV3Dofus } from '@/services/axiosInstance'

export const useFetchWorkshopJobs = async () => {
  return await clientV3Dofus.get('/dofus/workshop/jobs')
}

export const useAddJobsToWorkshop = async (workshopId: number, jobIds: number[]) => {
  return await clientV3Dofus.post(`/dofus/workshops/${workshopId}/jobs`, jobIds)
}

export const useRemoveJobFromWorkshop = async (workshopId: number, jobId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/jobs/${jobId}`)
}
