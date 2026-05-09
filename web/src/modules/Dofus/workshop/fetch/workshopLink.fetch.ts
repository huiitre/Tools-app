import { clientV3Dofus } from '@/services/axiosInstance';
import { WorkshopLinkSource } from '@/modules/Dofus/workshop/types/workshop.types';

export const useAddWorkshopLink = async (
  workshopId: number,
  data: { url: string; source: WorkshopLinkSource },
) => {
  return await clientV3Dofus.post(`/dofus/workshops/${workshopId}/links`, data);
};

export const useUpdateWorkshopLink = async (
  workshopId: number,
  linkId: number,
  data: { url: string; label: string },
) => {
  return await clientV3Dofus.put(`/dofus/workshops/${workshopId}/links/${linkId}`, data);
};

export const useDeleteWorkshopLink = async (workshopId: number, linkId: number) => {
  return await clientV3Dofus.delete(`/dofus/workshops/${workshopId}/links/${linkId}`);
};
