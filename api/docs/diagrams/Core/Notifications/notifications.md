# Core / Notifications

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  InternalNotificationsController["InternalNotificationsController"]
  NotificationsController["NotificationsController"]
  PublishNotificationRequest["PublishNotificationRequest"]
  PublishNotificationResponse["PublishNotificationResponse"]
  SendNotificationResponse["SendNotificationResponse"]
  end
  subgraph Application
  DeleteNotificationsUseCase["DeleteNotificationsUseCase"]
  GetMyNotificationsUseCase["GetMyNotificationsUseCase"]
  INotificationRepository(["INotificationRepository"])
  MarkNotificationsAsReadUseCase["MarkNotificationsAsReadUseCase"]
  NotificationService["NotificationService"]
  NotificationType["NotificationType"]
  NotificationTypes["NotificationTypes"]
  NotificationView["NotificationView"]
  PublishInternalNotificationUseCase["PublishInternalNotificationUseCase"]
  SendNotificationCommand["SendNotificationCommand"]
  SendNotificationUseCase["SendNotificationUseCase"]
  end
  subgraph Infrastructure
  PostgresNotificationRepository["PostgresNotificationRepository"]
  end
  subgraph Autre
  NotificationsModule["NotificationsModule"]
  end
  DeleteNotificationsUseCase --> INotificationRepository
  GetMyNotificationsUseCase --> INotificationRepository
  InternalNotificationsController --> PublishInternalNotificationUseCase
  MarkNotificationsAsReadUseCase --> INotificationRepository
  NotificationService --> INotificationRepository
  NotificationsController --> DeleteNotificationsUseCase
  NotificationsController --> GetMyNotificationsUseCase
  NotificationsController --> MarkNotificationsAsReadUseCase
  NotificationsController --> SendNotificationUseCase
  PostgresNotificationRepository -.-> INotificationRepository
  PublishInternalNotificationUseCase --> NotificationService
  SendNotificationCommand --> NotificationType
  SendNotificationUseCase --> NotificationService
```
