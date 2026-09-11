using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Lit ce que le sync a enregistré : ni le serveur de jeu ni les hébergeurs de mods ne sont
// contactés, les icônes ayant été résolues au sync.
public sealed class GetGameServerModsUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerDashboardRepository gameServerDashboardRepository) : SecuredUseCase(authorizer)
{
    public async Task<GameServerModsView> Execute(string slug) =>
        await gameServerDashboardRepository.FindModsBySlugAsync(slug)
        ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", $"Aucun serveur de jeu visible pour le slug « {slug} ».");
}
