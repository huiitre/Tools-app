using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Implémentée en plus d'IGameServerProvider par les jeux qui acceptent des commandes
// d'administration. Chaque jeu déclare les siennes : deux jeux n'ont ni les mêmes actions, ni les
// mêmes paramètres, et aucun code d'action n'est connu du module.
public interface IGameServerActions
{
    IReadOnlyList<GameServerActionDefinition> Actions { get; }

    Task ExecuteAsync(
        GameServerTarget target,
        string actionCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken);
}
