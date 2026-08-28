using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Orchestration du poll hors HTTP. Une panne d'un serveur est isolée : les autres cibles sont
// toujours interrogées et écrites en base pendant le même passage.
public sealed class PollGameServersUseCase(
    IGameServerPollingRepository gameServerPollingRepository,
    IEnumerable<IGameServerProvider> gameProviders,
    ILogger<PollGameServersUseCase> logger)
{
    // Les clients UDP et TCP n'ont pas de timeout propre
    // et ne dépendent que du token reçu ici : sans cette borne, un seul serveur qui ne répond
    // jamais (paquet droppé, firewall silencieux) fige indéfiniment cet appel — et comme la
    // boucle ci-dessous est séquentielle, ça gèle aussi tous les serveurs suivants et tous les
    // passages futurs, sans jamais lever d'exception ni loguer quoi que ce soit.
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(15);

    private readonly IReadOnlyDictionary<string, IGameServerProvider> providers = gameProviders
        .ToDictionary(provider => provider.GameCode, StringComparer.Ordinal);

    public async Task Execute(CancellationToken cancellationToken)
    {
        var gameServers = await gameServerPollingRepository.FindAllForPollingAsync();

        // En parallèle plutôt qu'en séquence : avec un timeout de 15s par serveur, une boucle
        // séquentielle ferait grimper la durée d'un passage avec le nombre de serveurs (8 serveurs
        // down = jusqu'à 2 minutes), dépassant l'intervalle de 60s entre deux passages. En
        // parallèle, un passage complet est borné à ~15s quel que soit le nombre de serveurs.
        await Task.WhenAll(gameServers.Select(gameServer => PollOneAsync(gameServer, cancellationToken)));
    }

    private async Task PollOneAsync(GameServerTarget gameServer, CancellationToken cancellationToken)
    {
        try
        {
            if (!providers.TryGetValue(gameServer.GameCode, out var provider))
            {
                logger.LogError(
                    "Aucun provider n'est enregistré pour le jeu {GameCode} (serveur {Slug}).",
                    gameServer.GameCode,
                    gameServer.Slug);
                await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, GameServerStatus.Offline);
                return;
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(PerServerTimeout);

            var status = await provider.FetchStatusAsync(gameServer, timeoutSource.Token);
            await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, status);

            logger.LogInformation(
                "Poll {Slug} ({GameCode}) : connexion {ConnectionResult}, {NumPlayers}/{MaxPlayers} joueurs.",
                gameServer.Slug,
                gameServer.GameCode,
                status.Online ? "réussie" : "échouée",
                status.NumPlayers?.ToString() ?? "?",
                status.MaxPlayers?.ToString() ?? "?");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Échec du poll du serveur {Slug} ({GameCode}).",
                gameServer.Slug,
                gameServer.GameCode);

            // Même un adapter défaillant laisse une trace fraîche et explicite en base.
            try
            {
                await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, GameServerStatus.Offline);
            }
            catch (Exception updateException)
            {
                logger.LogError(updateException,
                    "Impossible d'enregistrer le statut hors ligne du serveur {Slug}.", gameServer.Slug);
            }
        }
    }
}
