using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Le rôle READ_ONLY par défaut de SecuredUseCase suffit : un appel authentifié ne lit qu'un
// snapshot persistant, sans déclencher aucune connexion réseau vers les serveurs de jeu.
public sealed class GetGameServersUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerDashboardRepository gameServerDashboardRepository) : SecuredUseCase(authorizer)
{
    public Task<IReadOnlyList<GameServerDashboardView>> Execute()
    {
        return gameServerDashboardRepository.FindVisibleForDashboardAsync();
    }
}
