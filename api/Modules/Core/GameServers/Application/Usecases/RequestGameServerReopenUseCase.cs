using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.Notifications.Application;
using Tools.Api.Modules.Core.Notifications.Application.Services;
using Tools.Api.Modules.Core.Realtime.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Users.Application;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Un utilisateur demande la réouverture d'un serveur hors ligne : les administrateurs (ADMIN et
// au-dessus) sont notifiés. Une demande par serveur et par heure, tous utilisateurs confondus.
public sealed class RequestGameServerReopenUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerTargetRepository targetRepository,
    IGameServerLiveStateStore liveStateStore,
    IGameServerReopenRequestStore reopenRequestStore,
    IUserRepository userRepository,
    NotificationService notificationService,
    IRealtimePublisher realtimePublisher,
    ILogger<RequestGameServerReopenUseCase> logger) : SecuredUseCase(authorizer)
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(1);

    public async Task<GameServerReopenRequestView> Execute(string slug)
    {
        var gameServer = await targetRepository.FindBySlugAsync(slug)
            ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", "Serveur de jeu introuvable.");

        // L'état du dernier poll fait foi : pas encore interrogé ou en ligne, rien à rouvrir.
        var snapshot = liveStateStore.GetAll().FirstOrDefault(s => s.Slug == slug);
        if (snapshot is null || snapshot.Online)
            throw AppException.Conflict("GAME_SERVER_NOT_OFFLINE", "Ce serveur n'est pas hors ligne.");

        if (!reopenRequestStore.TryRequest(slug, DateTime.UtcNow, Cooldown, out var requestedAt))
            throw AppException.Conflict(
                "GAME_SERVER_REOPEN_ALREADY_REQUESTED",
                "Une demande de réouverture a déjà été envoyée il y a moins d'une heure.");

        var requester = await userRepository.FindProfileAsync(CurrentUser.UserId);

        try
        {
            await notificationService.Send(SendNotificationCommand.ForMinRole(
                RoleCode.Admin,
                "Demande de réouverture",
                $"{requester?.Name ?? "Un utilisateur"} a demandé la réouverture du serveur {gameServer.ServerName} ({gameServer.GameName ?? gameServer.GameCode}).",
                NotificationType.Warning,
                $$"""{"gameServerSlug":"{{slug}}"}"""));
        }
        catch (Exception error)
        {
            // Une demande qui n'a prévenu personne ne doit pas bloquer la suivante pendant une heure.
            logger.LogError(error, "Notification de réouverture impossible pour {Slug}", slug);
            reopenRequestStore.Cancel(slug, requestedAt);
            throw AppException.Unavailable(
                "GAME_SERVER_REOPEN_NOTIFICATION_FAILED", "La demande n'a pas pu être transmise. Réessaie plus tard.");
        }

        // Tous les écrans voient le bouton passer en « demandée » sans attendre le prochain poll.
        liveStateStore.Set(snapshot with { ReopenRequestedAt = requestedAt });
        await realtimePublisher.PublishToAllAsync(PollGameServersUseCase.LiveUpdatedEvent, liveStateStore.GetAll());

        return new GameServerReopenRequestView(requestedAt, requestedAt + Cooldown);
    }
}
