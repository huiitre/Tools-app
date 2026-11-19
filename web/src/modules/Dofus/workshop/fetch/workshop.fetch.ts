import { clientV3Dofus } from '@/services/axiosInstance';
import type { WorkshopLinkSource } from '@/modules/Dofus/workshop/types/workshop.types';

export const useFetchWorkshops = async () => {
  return await clientV3Dofus.get('/dofus/workshops');
};

export const useFetchWorkshopDetail = async (workshopId: number) => {
  return await clientV3Dofus.get(`/dofus/workshops/${workshopId}`);
};

export const useCreateWorkshop = async (data: {
  name: string;
  tagIds?: number[];
}) => {
  return await clientV3Dofus.post('/dofus/workshops', data);
};

export const useUpdateWorkshop = async (
  workshopId: number,
  data: { name?: string; isActive?: boolean; tagIds?: number[] },
) => {
  return await clientV3Dofus.patch(`/dofus/workshops/${workshopId}`, data);
};

export const useDeleteWorkshop = async (workshopId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}`);
};

export type ImportWorkshopBody = {
  name: string
  breedIds: number[]
  jobIds: number[]
  tagIds: number[]
  links: { source: WorkshopLinkSource; url: string; label?: string }[]
}

export const useImportWorkshop = async (workshopId: number, body: ImportWorkshopBody) => {
  return await clientV3Dofus.post(`/dofus/workshops/${workshopId}/import`, body);
}
