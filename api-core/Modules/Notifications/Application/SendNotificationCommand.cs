using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Notifications.Application;

// Envoi d'une notification. La cible est décrite par l'un des deux critères ci-dessous,
// jamais les deux — d'où les fabriques, qui rendent l'intention explicite à l'appel.
//
// Seuls ces deux ciblages sont portés pour l'instant : ce sont les seuls que l'API Java
// utilise réellement. Ses variantes `global()` et `module()` existent sans appelant et seront
// reprises lorsqu'un besoin apparaîtra.
public sealed record SendNotificationCommand(
    string Title,
    string Body,
    NotificationType Type,
    long? TargetUserId = null,
    RoleCode? TargetMinRole = null,

    // JSON libre consommé par le frontend, par exemple {"route": "valorant-shop"}.
    string? Metadata = null)
{
    public static SendNotificationCommand ForUser(
        long userId,
        string title,
        string body,
        NotificationType type,
        string? metadata = null) =>
        new(title, body, type, TargetUserId: userId, Metadata: metadata);

    // Destinataires : tous les comptes dont le rôle global atteint au moins `minRole`.
    public static SendNotificationCommand ForMinRole(
        RoleCode minRole,
        string title,
        string body,
        NotificationType type,
        string? metadata = null) =>
        new(title, body, type, TargetMinRole: minRole, Metadata: metadata);
}
