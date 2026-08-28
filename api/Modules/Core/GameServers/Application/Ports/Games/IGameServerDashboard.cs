using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Implémentée en plus d'IGameServerProvider par les jeux qui exposent assez d'informations pour
// un dashboard. Un jeu qui ne l'implémente pas n'en a tout simplement pas — rien à déclarer
// ailleurs, et aucune méthode à remplir de valeurs vides.
public interface IGameServerDashboard
{
    // Volet stable, chargé à l'ouverture.
    Task<GameServerDetailsView> FetchDetailsAsync(GameServerTarget target, CancellationToken cancellationToken);

    // Volet rafraîchi toutes les 5 s, en un seul appel quel que soit le jeu.
    Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken);
}
