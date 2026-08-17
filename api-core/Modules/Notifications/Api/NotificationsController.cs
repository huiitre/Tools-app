using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Notifications.Application.Usecases;
using Tools.ApiCore.Modules.Notifications.Application.Views;

namespace Tools.ApiCore.Modules.Notifications.Api;

// Notifications de l'utilisateur connecté : lecture, marquage, suppression, et envoi manuel.
//
// Les use cases sont résolus **par action** ([FromServices]) et non dans le constructeur du
// contrôleur. Un use case sécurisé vérifie les droits dès sa construction : injecté au niveau du
// contrôleur, il serait construit pour toutes les routes, y compris celles qui ne s'en servent
// pas, et son exigence de rôle s'appliquerait à elles — ici, l'envoi exige TECH quand la lecture
// se contente de READ_ONLY.
[ApiController]
[Route("notifications")]
public class NotificationsController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NotificationView>> GetMine(
        [FromServices] GetMyNotificationsUseCase getMyNotificationsUseCase)
    {
        return getMyNotificationsUseCase.Execute();
    }

    // Le corps est celui de la route interne : une notification se décrit de la même façon,
    // qu'elle vienne d'un humain ou d'un service. Seul le contrôle d'accès diffère.
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] PublishNotificationRequest request,
        [FromServices] SendNotificationUseCase sendNotificationUseCase)
    {
        var notificationId = await sendNotificationUseCase.Execute(request.ToCommand());

        // 204 quand la cible ne désigne personne : rien n'a été créé, il n'y a pas de ressource
        // à annoncer.
        return notificationId is { } id
            ? Created(string.Empty, new SendNotificationResponse(id))
            : NoContent();
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkAsRead(
        [FromServices] MarkNotificationsAsReadUseCase markNotificationsAsReadUseCase,
        [FromQuery] string? ids = null)
    {
        await markNotificationsAsReadUseCase.Execute(ParseIds(ids));
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteNotificationsUseCase deleteNotificationsUseCase,
        [FromQuery] string? ids = null)
    {
        await deleteNotificationsUseCase.Execute(ParseIds(ids));
        return NoContent();
    }

    // `?ids=1,2,3` : forme héritée de l'API Java, que le frontend envoie déjà. ASP.NET ne découpe
    // pas une valeur sur les virgules — un `long[]` attendrait `?ids=1&ids=2` — donc la chaîne est
    // lue telle quelle puis découpée ici. Paramètre absent ou vide : toutes les notifications.
    private static IReadOnlyCollection<long>? ParseIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return null;
        }

        var parsed = new List<long>();

        foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!long.TryParse(part, out var id))
            {
                throw AppException.Validation(
                    "INVALID_NOTIFICATION_IDS",
                    "Les identifiants de notification doivent être des nombres séparés par des virgules.");
            }

            parsed.Add(id);
        }

        return parsed.Count == 0 ? null : parsed;
    }
}

public sealed record SendNotificationResponse(long Id);
