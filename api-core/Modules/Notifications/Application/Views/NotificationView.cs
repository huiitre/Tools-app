using Tools.ApiCore.Modules.Notifications.Application;

public sealed record NotificationView(
    long Id,
    string Title,
    string Body,
    NotificationType Type,
    string Metadata,
    DateTime CreatedAt,
    bool Read
);