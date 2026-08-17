namespace Tools.Api.Modules.Notifications.Application;

// Nature d'une notification, telle que le frontend la met en forme.
// Les valeurs sont stockées en clair dans tools_core.notifications.type et doivent rester
// identiques à celles de l'API Java tant que les deux écrivent dans la même table.
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public static class NotificationTypes
{
    public static string ToCode(this NotificationType type) => type switch
    {
        NotificationType.Info => "INFO",
        NotificationType.Success => "SUCCESS",
        NotificationType.Warning => "WARNING",
        NotificationType.Error => "ERROR",
        _ => "INFO"
    };
}
