using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;
using Tools.Api.Modules.Core.Realtime.Application.Ports;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Orchestration du poll hors HTTP. Une panne d'un serveur est isolée : les autres cibles sont
// toujours interrogées pendant le même passage.
//
// Écrit à deux endroits, le temps de la transition vers le push WebSocket (voir la mémoire projet
// GameServers → push WebSocket) : la base (comme avant, pour la home actuelle qui la relit encore)
// et le nouvel état en mémoire (IGameServerLiveStateStore), destiné au push et à une lecture
// immédiate sans attendre le prochain tick (voir GetGameServersLiveStateUseCase). La base
// disparaîtra de cette boucle une fois le front basculé sur le WebSocket ; elle n'est pas retirée
// maintenant pour ne rien casser en cours de route.
public sealed class PollGameServersUseCase(
    IGameServerPollingRepository gameServerPollingRepository,
    IGameServerLiveStateStore liveStateStore,
    IRealtimePublisher realtimePublisher,
    IEnumerable<IGameServerProvider> gameProviders,
    ILogger<PollGameServersUseCase> logger)
{
    // Nom de l'event écouté côté front. Diffusé à tout le monde (v1, pas de groupe) : la donnée
    // est publique à tout utilisateur authentifié, personne n'en reçoit une version différente.
    public const string LiveUpdatedEvent = "Core.GameServersLiveUpdated";

    // Les clients UDP et TCP n'ont pas de timeout propre et ne dépendent que du token reçu ici :
    // sans cette borne, un seul serveur qui ne répond jamais (paquet droppé, firewall silencieux)
    // fige indéfiniment cet appel. Bien en dessous de l'intervalle de 10s entre deux passages
    // (voir GameServersPollingService), pour qu'un serveur muet ne fasse jamais déborder un tick
    // sur le suivant.
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(4);

    private readonly IReadOnlyDictionary<string, IGameServerProvider> providers = gameProviders
        .ToDictionary(provider => provider.GameCode, StringComparer.Ordinal);

    // Seuls les jeux qui implémentent IGameServerDashboard ont un FetchLiveAsync à appeler : les
    // autres n'ont que le statut léger de IGameServerProvider.
    private readonly IReadOnlyDictionary<string, IGameServerDashboard> dashboards = gameProviders
        .OfType<IGameServerDashboard>()
        .ToDictionary(provider => ((IGameServerProvider)provider).GameCode, StringComparer.Ordinal);

    public async Task Execute(CancellationToken cancellationToken)
    {
        var gameServers = await gameServerPollingRepository.FindAllForPollingAsync();

        // En parallèle plutôt qu'en séquence : avec un timeout de 4s par serveur, une boucle
        // séquentielle ferait grimper la durée d'un passage avec le nombre de serveurs, dépassant
        // l'intervalle de 10s entre deux passages. En parallèle, un passage complet est borné à
        // ~4s quel que soit le nombre de serveurs.
        await Task.WhenAll(gameServers.Select(gameServer => PollOneAsync(gameServer, cancellationToken)));

        // Un seul message par tick avec l'état de tous les serveurs, pas un par serveur : une vue
        // cohérente d'un coup côté front plutôt que des mises à jour qui arrivent en ordre dispersé
        // sur la fenêtre de ~4s du passage.
        await realtimePublisher.PublishToAllAsync(LiveUpdatedEvent, liveStateStore.GetAll());
    }

    // Rafraîchit un seul serveur tout de suite, sans attendre le prochain tick — appelé après une
    // action d'administration (kick/ban/annonce, commande libre) pour que son effet soit visible
    // immédiatement plutôt que sous 10s. Quelques admins qui cliquent occasionnellement ne pèsent
    // rien face au serveur de jeu ; ce n'est pas le même levier de charge que le poll périodique.
    public async Task RefreshOneAsync(GameServerTarget gameServer, CancellationToken cancellationToken)
    {
        await PollOneAsync(gameServer, cancellationToken);
        await realtimePublisher.PublishToAllAsync(LiveUpdatedEvent, liveStateStore.GetAll());
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
                await MarkOfflineAsync(gameServer);
                return;
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(PerServerTimeout);

            // Un seul appel réseau par serveur : FetchLiveAsync contient déjà le nombre de
            // joueurs que la home affiche, pas la peine d'appeler aussi FetchStatusAsync.
            GameServerLiveView? live = null;
            bool online;
            int? numPlayers;
            int? maxPlayers;
            if (dashboards.TryGetValue(gameServer.GameCode, out var dashboard))
            {
                // Pas de champ Online sur GameServerLiveView : atteindre cette ligne sans lever
                // veut dire que la connexion a réussi, une panne réseau étant remontée en
                // exception (cf. CobblemonProvider/ArkProvider), donc rattrapée plus bas.
                live = await dashboard.FetchLiveAsync(gameServer, timeoutSource.Token);
                online = true;
                numPlayers = live.PlayerCount;
                maxPlayers = live.MaxPlayers;
            }
            else
            {
                // Contrairement au cas ci-dessus, certains providers (Humanitz, Steam A2S,
                // Palworld) signalent une panne par un retour propre (GameServerStatus.Offline),
                // sans lever : Online doit venir de ce retour, jamais être supposé vrai.
                var status = await provider.FetchStatusAsync(gameServer, timeoutSource.Token);
                online = status.Online;
                numPlayers = status.NumPlayers;
                maxPlayers = status.MaxPlayers;
            }

            await gameServerPollingRepository.UpdateStatusAsync(gameServer.Id, new GameServerStatus(online, numPlayers, maxPlayers));
            liveStateStore.Set(new GameServerLiveSnapshot(gameServer.Slug, online, numPlayers, maxPlayers, DateTime.UtcNow, live));

            logger.LogInformation(
                "Poll {Slug} ({GameCode}) : connexion {ConnectionResult}, {NumPlayers}/{MaxPlayers} joueurs.",
                gameServer.Slug,
                gameServer.GameCode,
                online ? "réussie" : "échouée",
                numPlayers?.ToString() ?? "?",
                maxPlayers?.ToString() ?? "?");
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

            await MarkOfflineAsync(gameServer);
        }
    }

    private async Task MarkOfflineAsync(GameServerTarget gameServer)
    {
        liveStateStore.Set(GameServerLiveSnapshot.Unreachable(gameServer.Slug));

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
