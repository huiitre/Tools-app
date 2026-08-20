using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Realtime.Application;
using Tools.Api.Modules.Core.Realtime.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Realtime.Api.Internal;

// Relais d'événement temps réel pour un appelant de service à service, authentifié par secret partagé.
[ApiController]
[Route("internal/realtime/publish")]
[AllowAnonymous]
[InternalApi]
public sealed class InternalRealtimePublishController(PublishRealtimeEventUseCase publishRealtimeEventUseCase)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish(PublishRealtimeEventRequest request)
    {
        await publishRealtimeEventUseCase.Execute(request.ToCommand());
        return NoContent();
    }
}

// Payload opaque : le Core ne connaît ni sa forme ni sa signification, il ne fait que le relayer.
public sealed record PublishRealtimeEventRequest(
    [Required] string EventType,
    [Required] JsonElement Payload,
    long? TargetUserId,
    string? TargetMinRole,
    long? TargetModuleId,
    IReadOnlyCollection<long>? TargetUserIds)
{
    public PublishRealtimeEventCommand ToCommand()
    {
        if (TargetUserIds is { Count: > 0 })
        {
            return new PublishRealtimeEventCommand(EventType, Payload, TargetUserIds: TargetUserIds);
        }

        if (TargetUserId is { } userId)
        {
            return new PublishRealtimeEventCommand(EventType, Payload, TargetUserId: userId);
        }

        if (TargetMinRole is not null)
        {
            var minRole = RoleCodes.Parse(TargetMinRole)
                ?? throw AppException.Validation(
                    "INVALID_TARGET_ROLE",
                    "Le rôle minimum indiqué est inconnu.");

            return new PublishRealtimeEventCommand(EventType, Payload, TargetMinRole: minRole);
        }

        if (TargetModuleId is { } moduleId)
        {
            return new PublishRealtimeEventCommand(EventType, Payload, TargetModuleId: moduleId);
        }

        throw AppException.Validation(
            "MISSING_REALTIME_TARGET",
            "Un destinataire, un rôle minimum, un module ou une liste d'utilisateurs est obligatoire.");
    }
}
