using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Realtime.Application.Ports;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Realtime.Application.Usecases;

// Action déclenchée par un appel de service à service, sans utilisateur à autoriser.
public sealed class PublishRealtimeEventUseCase(
    IRecipientResolver recipientResolver,
    IRealtimePublisher realtimePublisher)
{
    public async Task Execute(PublishRealtimeEventCommand command)
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
