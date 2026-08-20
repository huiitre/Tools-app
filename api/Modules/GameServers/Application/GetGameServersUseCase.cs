using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;

namespace Tools.Api.Modules.GameServers.Application;

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
