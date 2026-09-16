namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Dernier état connu d'un serveur, gardé en mémoire par IGameServerLiveStateStore. Online,
// NumPlayers et MaxPlayers sont toujours renseignés, quel que soit le jeu — c'est ce que la home
// affiche. Live ne l'est que pour les jeux qui implémentent IGameServerDashboard et seulement si
// le poll a réussi ; un jeu qui n'a pas de dashboard, ou un poll en échec, le laisse à null.
public sealed record GameServerLiveSnapshot(
    string Slug,
    bool Online,
    int? NumPlayers,
    int? MaxPlayers,
    DateTime CheckedAt,
    GameServerLiveView? Live)
{
    public static GameServerLiveSnapshot Unreachable(string slug) =>
        new(slug, false, null, null, DateTime.UtcNow, null);
}
