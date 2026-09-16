using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// État live gardé en mémoire, alimenté par le poll (toutes les 10s) et lu par tout ce qui a
// besoin de l'état courant sans attendre le prochain tick — typiquement une connexion WebSocket
// qui vient de s'établir. Enregistrée en Singleton : une seule instance pour toute l'application,
// jamais une par requête, sinon chaque lecteur verrait un état vide plutôt que celui du poll.
public interface IGameServerLiveStateStore
{
    void Set(GameServerLiveSnapshot snapshot);

    // Null si aucun poll n'a encore eu lieu pour ce serveur depuis le démarrage de l'API.
    GameServerLiveSnapshot? Get(string slug);

    IReadOnlyList<GameServerLiveSnapshot> GetAll();
}
