import { clientCore } from '@/services/axiosInstance'
import type { CreateFeedbackRequest, Feedback, BatchDeleteRequest, UpdateReadStatusRequest } from '../types/feedback.types'

export async function createFeedback(message: string): Promise<void> {
  await clientCore.post<void>('/feedbacks', { message } as CreateFeedbackRequest)
}

export async function fetchAdminFeedbacks(): Promise<Feedback[]> {
  const { data } = await clientCore.get<Feedback[]>('/feedbacks/admin')
  return data
}

export async function batchDeleteFeedbacks(ids: number[]): Promise<void> {
  await clientCore.delete<void>('/feedbacks/admin', { data: { feedbackIds: ids } as BatchDeleteRequest })
}

export async function batchUpdateFeedbacksReadStatus(ids: number[], isRead: boolean): Promise<void> {
  await clientCore.patch<void>('/feedbacks/admin/read-status', { ids, isRead } as UpdateReadStatusRequest)
}
