using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Notifications.Application;

// Envoi d'une notification. La cible est décrite par un seul des critères ci-dessous — d'où les fabriques.
public sealed record SendNotificationCommand(
    string Title,
    string Body,
    NotificationType Type,
    long? TargetUserId = null,
    RoleCode? TargetMinRole = null,
    long? TargetModuleId = null,

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

    // Destinataires : tous les membres du module désigné.
    public static SendNotificationCommand ForModule(
        long moduleId,
        string title,
        string body,
        NotificationType type,
        string? metadata = null) =>
        new(title, body, type, TargetModuleId: moduleId, Metadata: metadata);
}
