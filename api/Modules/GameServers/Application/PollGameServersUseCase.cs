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
                        "Aucun adapter de statut n'est enregistré pour le protocole {ProtocolType} (serveur {GameServerId}).",
                        gameServer.ProtocolType,
                        gameServer.Id);
                    await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, GameServerStatus.Offline);
                    continue;
                }

                var status = await provider.FetchAsync(gameServer, cancellationToken);
                await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, status);
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
