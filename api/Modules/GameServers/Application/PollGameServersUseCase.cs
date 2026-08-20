using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Application;

// Orchestration du poll hors HTTP. Une panne d'un serveur est isolée : les autres cibles sont
// toujours interrogées et écrites en base pendant le même passage.
public sealed class PollGameServersUseCase(
    IGameServerPollingRepository gameServerPollingRepository,
    IEnumerable<IGameServerStatusProvider> statusProviders,
    ILogger<PollGameServersUseCase> logger)
{
    // SteamA2sStatusProvider (UDP) et SourceRconStatusProvider (TCP) n'ont pas de timeout propre
    // et ne dépendent que du token reçu ici : sans cette borne, un seul serveur qui ne répond
    // jamais (paquet droppé, firewall silencieux) fige indéfiniment cet appel — et comme la
    // boucle ci-dessous est séquentielle, ça gèle aussi tous les serveurs suivants et tous les
    // passages futurs, sans jamais lever d'exception ni loguer quoi que ce soit.
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(15);

    private readonly IReadOnlyDictionary<string, IGameServerStatusProvider> providers = statusProviders
        .ToDictionary(provider => provider.ProtocolType, StringComparer.Ordinal);

    public async Task Execute(CancellationToken cancellationToken)
    {
        var gameServers = await gameServerPollingRepository.FindAllForPollingAsync();

        foreach (var gameServer in gameServers)
        {
            try
            {
                if (!providers.TryGetValue(gameServer.ProtocolType, out var provider))
                {
                    logger.LogError(
                        "Aucun adapter de statut n'est enregistré pour le protocole {ProtocolType} (serveur {Slug}).",
                        gameServer.ProtocolType,
                        gameServer.Slug);
                    await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, GameServerStatus.Offline);
                    continue;
                }

                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(PerServerTimeout);

                var status = await provider.FetchAsync(gameServer, timeoutSource.Token);
                await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, status);

                logger.LogInformation(
                    "Poll {Slug} ({ProtocolType}) : connexion {ConnectionResult}, {NumPlayers}/{MaxPlayers} joueurs.",
                    gameServer.Slug,
                    gameServer.ProtocolType,
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
                    "Échec du poll du serveur {GameServerId} via {ProtocolType}.",
                    gameServer.Id,
                    gameServer.ProtocolType);

                // Même un adapter défaillant laisse une trace fraîche et explicite en base.
                try
                {
                    await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, GameServerStatus.Offline);
                }
                catch (Exception updateException)
                {
                    logger.LogError(updateException,
                        "Impossible d'enregistrer le statut hors ligne du serveur {GameServerId}.", gameServer.Id);
                }
            }
        }
    }
}
