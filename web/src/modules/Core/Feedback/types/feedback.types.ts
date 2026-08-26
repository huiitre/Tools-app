export interface Feedback {
  id: number;
  userId: number;
  userName: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface CreateFeedbackRequest {
  message: string;
}

export interface BatchDeleteRequest {
  feedbackIds: number[];
}

export interface UpdateReadStatusRequest {
  ids: number[];
  isRead: boolean;
}
