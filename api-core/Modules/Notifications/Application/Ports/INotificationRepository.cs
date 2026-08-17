using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Notifications.Application.Ports;

public interface INotificationRepository
{
    // Enregistre le message source et retourne son identifiant. Les critères de ciblage sont
    // conservés tels quels : ils ne servent pas à la lecture mais à retracer l'intention.
    Task<long> CreateAsync(
        string title,
        string body,
        string type,
        long? targetUserId,
        long? targetModuleId,
        string? metadata);

    // Comptes dont le rôle global atteint au moins l'un des codes fournis, hors comptes TECH.
    Task<IReadOnlyList<long>> FindRecipientsByRoleCodesAsync(IReadOnlyCollection<string> roleCodes);

    // Membres du module désigné, hors comptes TECH.
    Task<IReadOnlyList<long>> FindRecipientsByModuleIdAsync(long moduleId);

    // Une ligne par destinataire : c'est elle qui porte l'état de lecture.
    Task AddRecipientsAsync(long notificationId, IReadOnlyCollection<long> userIds);

    Task<bool> UserExistsAsync(long userId);
}
