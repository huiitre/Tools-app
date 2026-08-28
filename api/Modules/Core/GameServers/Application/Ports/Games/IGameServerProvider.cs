using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Un fichier par jeu, résolu par gameCode : c'est le seul endroit du module qui sait parler à ce
// jeu, aussi bien pour le scheduler que pour le dashboard. Il reçoit sa cible en argument et ne
// connaît donc aucune adresse, ce qui lui permet de servir plusieurs serveurs du même jeu.
public interface IGameServerProvider
{
    string GameCode { get; }

    // Appelé par le scheduler toutes les 60 s. Volontairement minimal : c'est ce qui alimente la
    // table, donc le widget.
    Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken);
}
