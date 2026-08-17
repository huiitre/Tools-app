using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.ApiCore.Modules.Common.Api.Internal;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Notifications.Application;
using Tools.ApiCore.Modules.Notifications.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Notifications.Api;

// Publication de notification pour un appelant de service à service, authentifié par secret partagé.
[ApiController]
[Route("internal/notifications")]
[AllowAnonymous]
[InternalApi]
public class InternalNotificationsController(PublishInternalNotificationUseCase publishInternalNotificationUseCase)
    : ControllerBase
{
    // 200 + id si créée, 204 si aucun destinataire n'a été trouvé.
    [HttpPost]
    public async Task<IActionResult> Publish(PublishNotificationRequest request)
    {
        var notificationId = await publishInternalNotificationUseCase.Execute(request.ToCommand());
        return notificationId is { } id ? Ok(new PublishNotificationResponse(id)) : NoContent();
    }
}

public sealed record PublishNotificationResponse(long Id);

// DTO entrant : un destinataire précis, une population par rôle minimum, ou les membres d'un module.
public sealed record PublishNotificationRequest(
    [Required] string Title,
    [Required] string Body,
    string? Type,
    long? TargetUserId,
    string? TargetMinRole,
    long? TargetModuleId,
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

        if (TargetModuleId is { } moduleId)
        {
            return SendNotificationCommand.ForModule(moduleId, Title, Body, type, Metadata);
        }

        throw AppException.Validation(
            "MISSING_NOTIFICATION_TARGET",
            "Un destinataire, un rôle minimum ou un module est obligatoire.");
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
