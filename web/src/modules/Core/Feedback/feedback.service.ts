import { clientCore } from '@/services/axiosInstance';
import type { CreateFeedbackRequest, Feedback, BatchDeleteRequest, UpdateReadStatusRequest } from './feedback.types';

export const feedbackService = {
  async create(message: string): Promise<void> {
    await clientCore.post<void>('/feedbacks', { message } as CreateFeedbackRequest);
  },

  async getAllAdmin(): Promise<Feedback[]> {
    const { data } = await clientCore.get<Feedback[]>('/feedbacks/admin');
    return data;
  },

  async batchDelete(ids: number[]): Promise<void> {
    await clientCore.delete<void>('/feedbacks/admin', { data: { feedbackIds: ids } as BatchDeleteRequest });
  },

  async batchUpdateReadStatus(ids: number[], isRead: boolean): Promise<void> {
    await clientCore.patch<void>('/feedbacks/admin/read-status', { ids, isRead } as UpdateReadStatusRequest);
  }
};
