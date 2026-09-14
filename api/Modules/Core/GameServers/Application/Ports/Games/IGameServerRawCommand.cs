using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Implémentée en plus d'IGameServerProvider par les jeux dont le protocole d'administration
// accepte n'importe quelle commande texte (RCON) : c'est la même connexion que les actions et le
// direct, sans liste de commandes prédéfinie. Le use case ne connaît aucune commande et réserve
// l'accès au rôle le plus élevé, la commande elle-même échappant à tout contrôle.
public interface IGameServerRawCommand
{
    Task<string?> ExecuteRawCommandAsync(GameServerTarget target, string command, CancellationToken cancellationToken);
}
