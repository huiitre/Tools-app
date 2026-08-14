using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Password;

// Nettoyage planifié des jetons de réinitialisation expirés, toutes les 30 minutes.
//
// Un BackgroundService est un singleton : il ne peut pas dépendre directement de services
// scoped et crée donc son propre scope à chaque passage. Il n'appelle aucun use case
// sécurisé : hors requête HTTP, aucun utilisateur n'est identifié.
public sealed class PasswordResetCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<PasswordResetCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPasswordResetRepository>();

            var deleted = await repository.DeleteExpiredAsync(DateTime.UtcNow, cancellationToken);
            logger.LogInformation("Jetons de réinitialisation expirés supprimés : {Count}", deleted);
        }
        catch (OperationCanceledException)
        {
            // Arrêt de l'application : rien à signaler.
        }
        catch (Exception exception)
        {
            // Un échec ne doit jamais interrompre la boucle de nettoyage.
            logger.LogError(exception, "Échec du nettoyage des jetons de réinitialisation.");
        }
    }
}
