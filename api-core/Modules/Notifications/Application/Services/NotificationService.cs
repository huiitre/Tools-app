using Tools.ApiCore.Modules.Notifications.Application.Ports;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Notifications.Application.Services;

// Persiste une notification et lui résout ses destinataires.
//
// Ce n'est pas un use case sécurisé : il est appelé depuis des flux sans utilisateur
// authentifié — une inscription, par exemple, est le fait d'un visiteur anonyme. Le contrôle
// d'accès appartient aux use cases qui s'en servent, comme pour MailService.
//
// Le temps réel n'est pas de son ressort : la notification est enregistrée, et le destinataire
// la découvre au prochain chargement. Le push viendra avec SignalR, sans rien changer ici.
public sealed class NotificationService(
    INotificationRepository notificationRepository,
    ILogger<NotificationService> logger)
{
    // Retourne l'identifiant créé, nul si aucun destinataire n'a été trouvé.
    public async Task<long?> Send(SendNotificationCommand command)
    {
        var recipients = await ResolveRecipients(command);
        if (recipients.Count == 0)
        {
            // Sans destinataire, le message source serait un orphelin que personne ne lira.
            logger.LogWarning("Notification sans destinataire, ignorée : {Title}", command.Title);
            return null;
        }

        var notificationId = await notificationRepository.CreateAsync(
            command.Title,
            command.Body,
            command.Type.ToCode(),
            command.TargetUserId,
            command.Metadata);

        await notificationRepository.AddRecipientsAsync(notificationId, recipients);

        logger.LogInformation(
            "Notification {NotificationId} enregistrée pour {RecipientCount} destinataire(s) : {Title}",
            notificationId,
            recipients.Count,
            command.Title);

        return notificationId;
    }

    private async Task<IReadOnlyList<long>> ResolveRecipients(SendNotificationCommand command)
    {
        if (command.TargetUserId is { } userId)
        {
            return await notificationRepository.UserExistsAsync(userId) ? [userId] : [];
        }

        if (command.TargetMinRole is { } minRole)
        {
            return await notificationRepository.FindRecipientsByRoleCodesAsync(
                RoleCodes.CodesAtOrAbove(minRole));
        }

        // Aucun critère : le ciblage global n'a pas été porté, faute d'appelant.
        throw new InvalidOperationException(
            "Une notification doit désigner un utilisateur ou un rôle minimum.");
    }
}
