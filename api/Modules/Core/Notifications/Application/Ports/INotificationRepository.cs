using Tools.Api.Modules.Core.Notifications.Application.Views;

namespace Tools.Api.Modules.Core.Notifications.Application.Ports;

public interface INotificationRepository
{
    // Les cinquante dernières notifications de l'utilisateur, la plus récente d'abord. La limite
    // vient de l'API Java : la cloche du frontend affiche un historique court, pas un journal.
    Task<IReadOnlyList<NotificationView>> FindActiveForUserAsync(long userId);

    // `notificationIds` nul ou vide : toutes les notifications non lues de l'utilisateur.
    Task MarkAsReadAsync(long userId, IReadOnlyCollection<long>? notificationIds);

    // `notificationIds` nul ou vide : toutes les notifications de l'utilisateur.
    //
    // La suppression est physique et ne porte que sur `user_notifications` : le message source
    // reste, il appartient aux autres destinataires.
    Task DeleteAsync(long userId, IReadOnlyCollection<long>? notificationIds);

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
