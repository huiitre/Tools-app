using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// État live courant de tous les serveurs, pour un client qui vient d'établir sa connexion
// WebSocket et ne doit pas attendre le prochain tick du poll (jusqu'à 10s) pour afficher quelque
// chose. Lecture pure du même état en mémoire que PollGameServersUseCase alimente.
public sealed class GetGameServersLiveStateUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerLiveStateStore liveStateStore) : SecuredUseCase(authorizer)
{
    public IReadOnlyList<GameServerLiveSnapshot> Execute() => liveStateStore.GetAll();
}
