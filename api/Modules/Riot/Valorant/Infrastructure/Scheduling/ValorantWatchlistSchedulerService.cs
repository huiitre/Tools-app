using Tools.Api.Modules.Riot.Valorant.Application.User.Services;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure.Scheduling;

// Passe quotidienne : archive la boutique de chaque compte lié et prévient des skins suivis.
//
// **Elle résout le notifieur, jamais le use case de déclenchement.** Ce dernier est un
// SecuredUseCase : le construire ici échouerait, aucun utilisateur n'étant authentifié sur ce
// thread. C'est le piège que le scheduler Java porte encore.
//
// Le BackgroundService est un singleton : un scope est créé à chaque passage pour en résoudre les
// services scoped.
public sealed class ValorantWatchlistSchedulerService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<ValorantWatchlistSchedulerService> logger) : BackgroundService
{
    // 6 h UTC, comme le cron du Java. Le premier passage a lieu au démarrage, pour rattraper une
    // journée manquée après un redéploiement.
    private static readonly TimeOnly DailyRunTime = new(6, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ValorantWatchlistSchedulerService démarré, passage quotidien à {RunTime} UTC.", DailyRunTime);

        try
        {
            // **Pas de rattrapage en développement.** La passe appelle Riot pour chaque compte lié
            // de chaque utilisateur et fait tourner leurs refresh tokens : avec « dotnet watch »,
            // qui relance le process à chaque sauvegarde, c'est une rafale d'appels sur des comptes
            // réels. Le rattrapage garde tout son sens en QA et en production, où un démarrage est
            // un redéploiement.
            if (environment.IsDevelopment())
            {
                logger.LogInformation("Rattrapage Valorant ignoré en développement.");
            }
            else
            {
                await RunAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(DelayUntilNextRun(), stoppingToken);
                await RunAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt propre de l'application.
        }
        catch (Exception exception)
        {
            // BackgroundService avale une exception qui sort d'ExecuteAsync : sans ce catch, la
            // passe quotidienne s'arrêterait définitivement sans laisser de trace.
            logger.LogError(exception, "ValorantWatchlistSchedulerService s'est arrêté suite à une erreur inattendue.");
        }
    }

    private static TimeSpan DelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var next = now.Date.Add(DailyRunTime.ToTimeSpan());

        return next <= now ? next.AddDays(1) - now : next - now;
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ValorantWatchlistNotifier>();
            await notifier.ProcessAllAccounts();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt propre de l'application.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Échec global du passage quotidien Valorant.");
        }
    }
}
