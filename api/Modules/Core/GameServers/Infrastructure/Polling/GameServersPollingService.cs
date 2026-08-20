using Tools.Api.Modules.Core.GameServers.Application;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;

// Le BackgroundService est singleton : un scope est créé à chaque passage avant de résoudre le
// use case scoped et son repository PostgreSQL. Le premier poll est immédiat, puis toutes les 60 s.
public sealed class GameServersPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<GameServersPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GameServersPollingService démarré, intervalle {IntervalSeconds}s.", Interval.TotalSeconds);

        try
        {
            using var timer = new PeriodicTimer(Interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PollAsync(stoppingToken);

                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt propre de l'application.
        }
        catch (Exception exception)
        {
            // BackgroundService avale silencieusement une exception non rattrapée qui sort
            // d'ExecuteAsync (le host ne s'arrête pas par défaut) : sans ce catch, le service
            // mourrait à la première itération sans laisser aucune trace.
            logger.LogError(exception, "GameServersPollingService s'est arrêté suite à une erreur inattendue.");
        }
    }

    private async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<PollGameServersUseCase>();
            await useCase.Execute(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt propre de l'application.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Échec global du passage de poll des serveurs de jeux.");
        }
    }
}
