export type NotificationType = 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR';

export interface AppNotification {
  id: number;
  title: string;
  body: string;
  type: NotificationType;
  metadata?: string;
  createdAt: string; // ISO string
  read: boolean;
}

export interface NotificationMetadata {
  route?: string;
  params?: Record<string, any>;
}
