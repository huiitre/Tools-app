using System.Collections.Concurrent;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Scheduling;

// Contrairement à GameServersPollingService (minuteur qui interroge tous les serveurs à
// intervalle fixe), ce service n'a rien à boucler : il est piloté par Schedule(...), appelé par
// GetGameServerDashboardUseCase quand un délai est demandé. ExecuteAsync ne fait que garder le
// token de durée de vie de l'application, utilisé par les comptes à rebours qu'il démarre.
//
// Un seul compte à rebours par serveur (clé Target.Id, pas GameCode : deux serveurs du même jeu
// sont indépendants). Un nouvel appel Schedule pour un serveur déjà en cours de compte à rebours
// annule l'ancien et démarre le nouveau — choix produit (voir api/docs/GAME_SERVER_ACTION_COUNTDOWN.md),
// pas une limitation technique.
public sealed class GameServerActionCountdownService(
    IServiceScopeFactory scopeFactory,
    ILogger<GameServerActionCountdownService> logger) : BackgroundService, IGameServerActionCountdownService
{
    // Paliers d'annonce, filtrés à ceux inférieurs au délai demandé. Un délai de 15s n'annonce
    // donc qu'à 10, 5, 4, 3, 2 et 1 seconde, jamais à 30/60/120/300.
    private static readonly int[] AnnounceCheckpoints = [300, 120, 60, 30, 10, 5, 4, 3, 2, 1];

    private readonly ConcurrentDictionary<long, CancellationTokenSource> countdownsByServerId = new();
    private CancellationToken applicationStoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        applicationStoppingToken = stoppingToken;
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt propre de l'application : les tâches de compte à rebours en cours sont liées
            // au même token, elles s'arrêtent d'elles-mêmes.
        }
    }

    public void Schedule(ScheduledGameServerAction scheduled)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(applicationStoppingToken);
        if (countdownsByServerId.TryRemove(scheduled.Target.Id, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }

        countdownsByServerId[scheduled.Target.Id] = cts;
        _ = RunAsync(scheduled, cts);
    }

    private async Task RunAsync(ScheduledGameServerAction scheduled, CancellationTokenSource cts)
    {
        var token = cts.Token;
        try
        {
            var remaining = scheduled.DelaySeconds;
            await AnnounceAsync(scheduled, remaining, token);

            foreach (var checkpoint in AnnounceCheckpoints.Where(checkpoint => checkpoint < remaining))
            {
                await Task.Delay(TimeSpan.FromSeconds(remaining - checkpoint), token);
                remaining = checkpoint;
                await AnnounceAsync(scheduled, remaining, token);
            }

            await Task.Delay(TimeSpan.FromSeconds(remaining), token);
            await ExecuteFinalActionAsync(scheduled, token);
        }
        catch (OperationCanceledException)
        {
            // Remplacé par un nouveau compte à rebours (Schedule) ou arrêt de l'application :
            // dans les deux cas, rien à journaliser, ce n'est pas un échec.
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Échec du compte à rebours pour l'action {ActionCode} sur {Slug}.",
                scheduled.ActionCode,
                scheduled.Target.Slug);
        }
        finally
        {
            ((ICollection<KeyValuePair<long, CancellationTokenSource>>)countdownsByServerId)
                .Remove(new KeyValuePair<long, CancellationTokenSource>(scheduled.Target.Id, cts));
        }
    }

    // Une annonce ratée ou absente (provider sans « announce ») ne doit jamais interrompre le
    // compte à rebours : seul un avertissement est journalisé. Le scope reste ouvert le temps de
    // l'appel : le provider résolu (Cobblemon est singleton, mais rien ne garantit qu'un futur
    // provider délayable le soit aussi) ne doit pas survivre à la fermeture de son scope.
    private async Task AnnounceAsync(ScheduledGameServerAction scheduled, int remainingSeconds, CancellationToken token)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var actionable = ResolveActionable(scope.ServiceProvider, scheduled.Target.GameCode);
            if (actionable is null || actionable.Actions.All(action => action.Code != "announce"))
            {
                return;
            }

            var message = $"Redémarrage dans {remainingSeconds} seconde{(remainingSeconds > 1 ? "s" : "")}.";
            await actionable.ExecuteAsync(
                scheduled.Target,
                "announce",
                new Dictionary<string, string> { ["message"] = message },
                token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Échec de l'annonce de compte à rebours pour {Slug} ({RemainingSeconds}s restantes).",
                scheduled.Target.Slug,
                remainingSeconds);
        }
    }

    // Contrairement à l'annonce, un échec ici est journalisé en erreur : c'est l'action que
    // l'admin a réellement demandée, elle ne doit jamais être silencieusement perdue.
    private async Task ExecuteFinalActionAsync(ScheduledGameServerAction scheduled, CancellationToken token)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var actionable = ResolveActionable(scope.ServiceProvider, scheduled.Target.GameCode)
                ?? throw new InvalidOperationException($"Aucun provider actionnable pour {scheduled.Target.GameCode}.");

            await actionable.ExecuteAsync(scheduled.Target, scheduled.ActionCode, scheduled.Parameters, token);

            var pollGameServersUseCase = scope.ServiceProvider.GetRequiredService<PollGameServersUseCase>();
            await pollGameServersUseCase.RefreshOneAsync(scheduled.Target, token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Échec de l'action différée {ActionCode} sur {Slug}.",
                scheduled.ActionCode,
                scheduled.Target.Slug);
        }
    }

    private static IGameServerActions? ResolveActionable(IServiceProvider serviceProvider, string gameCode) =>
        serviceProvider.GetServices<IGameServerProvider>()
            .OfType<IGameServerActions>()
            .FirstOrDefault(provider => ((IGameServerProvider)provider).GameCode == gameCode);
}
