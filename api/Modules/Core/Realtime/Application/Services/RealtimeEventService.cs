using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Realtime.Application.Ports;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Realtime.Application.Services;

// Résout les destinataires d'un event temps réel puis le pousse. Ce n'est pas un use case
// sécurisé : il n'a aucun accès à autoriser lui-même, le contrôle appartient à l'appelant
// (route interne authentifiée par secret, ou use case déjà sécurisé qui s'en sert en plus de
// son action principale) — même partage de responsabilité que NotificationService.
public sealed class RealtimeEventService(
    IRecipientResolver recipientResolver,
    IRealtimePublisher realtimePublisher)
{
    public async Task PublishAsync(PublishRealtimeEventCommand command)
    {
        var recipients = await ResolveRecipients(command);
        if (recipients.Count == 0)
        {
            return;
        }

        await realtimePublisher.PublishAsync(recipients, command.EventType, command.Payload);
    }

    private async Task<IReadOnlyList<long>> ResolveRecipients(PublishRealtimeEventCommand command)
    {
        if (command.TargetUserIds is { Count: > 0 } userIds)
        {
            return userIds.ToList();
        }

        if (command.TargetUserId is { } userId)
        {
            return await recipientResolver.UserExistsAsync(userId) ? [userId] : [];
        }

        if (command.TargetMinRole is { } minRole)
        {
            return await recipientResolver.FindByRoleCodesAsync(RoleCodes.CodesAtOrAbove(minRole));
        }

        if (command.TargetModuleId is { } moduleId)
        {
            return await recipientResolver.FindByModuleIdAsync(moduleId);
        }

        throw AppException.Validation(
            "MISSING_REALTIME_TARGET",
            "Un destinataire, un rôle minimum, un module ou une liste d'utilisateurs est obligatoire.");
    }
}
