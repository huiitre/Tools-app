using Tools.Api.Modules.GameServers.Application;

namespace Tools.Api.Modules.GameServers.Infrastructure.Polling;

// Le BackgroundService est singleton : un scope est créé à chaque passage avant de résoudre le
// use case scoped et son repository PostgreSQL. Le premier poll est immédiat, puis toutes les 60 s.
public sealed class GameServersPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<GameServersPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
