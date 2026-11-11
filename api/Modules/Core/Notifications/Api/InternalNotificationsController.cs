using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Notifications.Application;
using Tools.Api.Modules.Core.Notifications.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Notifications.Api;

// Ancienne URL des services (scripts du NAS, API Java), gardée le temps de leur migration : même
// use case que POST /notifications, avec la réponse d'origine (200 + id). À supprimer ensuite.
[ApiController]
[Route("internal/notifications")]
public class InternalNotificationsController : ControllerBase
{
    // 200 + id si créée, 204 si aucun destinataire n'a été trouvé.
    [HttpPost]
    public async Task<IActionResult> Publish(
        PublishNotificationRequest request,
        [FromServices] SendNotificationUseCase sendNotificationUseCase)
    {
        var notificationId = await sendNotificationUseCase.Execute(request.ToCommand());
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
