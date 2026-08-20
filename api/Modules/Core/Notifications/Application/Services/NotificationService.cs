using Tools.Api.Modules.Core.Notifications.Application.Ports;
using Tools.Api.Modules.Core.Realtime.Application.Ports;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Notifications.Application.Services;

// Persiste une notification, lui résout ses destinataires, et pousse en temps réel.
//
// Ce n'est pas un use case sécurisé : il est appelé depuis des flux sans utilisateur
// authentifié — une inscription, par exemple, est le fait d'un visiteur anonyme. Le contrôle
// d'accès appartient aux use cases qui s'en servent, comme pour MailService.
public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IRealtimePublisher realtimePublisher,
    ILogger<NotificationService> logger)
{
    private const string PushEventType = "ReceiveNotification";

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
            command.TargetModuleId,
            command.Metadata);

        await notificationRepository.AddRecipientsAsync(notificationId, recipients);

        logger.LogInformation(
            "Notification {NotificationId} enregistrée pour {RecipientCount} destinataire(s) : {Title}",
            notificationId,
            recipients.Count,
            command.Title);

        // Contrat consommé par le front : { id, title, body, type, metadata, createdAt, read }.
        await realtimePublisher.PublishAsync(recipients, PushEventType, new
        {
            id = notificationId,
            title = command.Title,
            body = command.Body,
            type = command.Type.ToCode(),
            metadata = command.Metadata,
            createdAt = DateTimeOffset.UtcNow,
            read = false
        });

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

        if (command.TargetModuleId is { } moduleId)
        {
            return await notificationRepository.FindRecipientsByModuleIdAsync(moduleId);
        }

        // Aucun critère : le ciblage global n'a pas été porté, faute d'appelant.
        throw new InvalidOperationException(
            "Une notification doit désigner un utilisateur, un rôle minimum ou un module.");
    }
}
