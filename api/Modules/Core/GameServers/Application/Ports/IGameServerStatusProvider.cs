using Tools.Api.Modules.Core.GameServers.Application.Dto;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports;

// Chaque adapter annonce le protocole qu'il sait interroger. Le use case ne connaît donc aucun
// gameCode : ajouter un jeu utilisant un protocole existant ne demande aucun changement ici.
public interface IGameServerStatusProvider
{
    string ProtocolType { get; }

    Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken);
}
