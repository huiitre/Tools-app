import { clientV3 } from '@/services/axiosInstance';
import type { CreateFeedbackRequest, Feedback, BatchDeleteRequest, UpdateReadStatusRequest } from './feedback.types';

export const feedbackService = {
  async create(message: string): Promise<void> {
    await clientV3.post<void>('/feedbacks', { message } as CreateFeedbackRequest);
  },

  async getAllAdmin(): Promise<Feedback[]> {
    const { data } = await clientV3.get<Feedback[]>('/feedbacks/admin');
    return data;
  },

  async batchDelete(ids: number[]): Promise<void> {
    await clientV3.delete<void>('/feedbacks/admin', { data: { ids } as BatchDeleteRequest });
  },

  async batchUpdateReadStatus(ids: number[], isRead: boolean): Promise<void> {
    await clientV3.patch<void>('/feedbacks/admin/read-status', { ids, isRead } as UpdateReadStatusRequest);
  }
};
