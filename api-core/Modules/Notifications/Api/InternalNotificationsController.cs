using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.ApiCore.Modules.Common.Api.Internal;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Notifications.Application;
using Tools.ApiCore.Modules.Notifications.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Notifications.Api;

// Publication d'une notification par un autre service : l'API Java pour ses modules métier,
// un extracteur du NAS, et demain le push realtime.
//
// AllowAnonymous est indispensable : la FallbackPolicy exige un utilisateur authentifié sur
// toute route non déclarée, et l'appelant ici est une machine qui n'agit au nom de personne.
// C'est InternalApi qui prend le relais du contrôle.
[ApiController]
[Route("internal/notifications")]
[AllowAnonymous]
[InternalApi]
public class InternalNotificationsController(PublishInternalNotificationUseCase publishInternalNotificationUseCase)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish(PublishNotificationRequest request)
    {
        await publishInternalNotificationUseCase.Execute(request.ToCommand());
        return NoContent();
    }
}

// DTO entrant. Le ciblage reprend celui de l'API Java : un destinataire précis, ou une
// population désignée par son rôle minimum.
public sealed record PublishNotificationRequest(
    [Required] string Title,
    [Required] string Body,
    string? Type,
    long? TargetUserId,
    string? TargetMinRole,
    string? Metadata)
{
    public SendNotificationCommand ToCommand()
    {
        var type = ParseType(Type);

        if (TargetUserId is { } userId)
        {
            return SendNotificationCommand.ForUser(userId, Title, Body, type, Metadata);
        }

        if (TargetMinRole is not null)
        {
            var minRole = RoleCodes.Parse(TargetMinRole)
                ?? throw AppException.Validation(
                    "INVALID_TARGET_ROLE",
                    "Le rôle minimum indiqué est inconnu.");

            return SendNotificationCommand.ForMinRole(minRole, Title, Body, type, Metadata);
        }

        throw AppException.Validation(
            "MISSING_NOTIFICATION_TARGET",
            "Un destinataire ou un rôle minimum est obligatoire.");
    }

    // Type omis : INFO, comme la valeur par défaut de la colonne.
    private static NotificationType ParseType(string? type) => type?.ToUpperInvariant() switch
    {
        null or "" or "INFO" => NotificationType.Info,
        "SUCCESS" => NotificationType.Success,
        "WARNING" => NotificationType.Warning,
        "ERROR" => NotificationType.Error,
        _ => throw AppException.Validation(
            "INVALID_NOTIFICATION_TYPE",
            "Le type de notification est inconnu.")
    };
}
