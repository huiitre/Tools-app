using Tools.Api.Modules.Auth.Application.Ports.Registration;

namespace Tools.Api.Modules.Auth.Infrastructure.Registration;

// Nettoyage planifié des inscriptions abandonnées, toutes les 30 minutes.
//
// Deux opérations distinctes : supprimer les comptes jamais confirmés dont le délai est
// écoulé, puis effacer les jetons expirés qui subsistent. L'ordre importe — supprimer les
// jetons d'abord rendrait indistinguables les inscriptions expirées de celles en cours.
//
// Ce service ne supprime jamais un compte sur le seul critère de is_active : un compte
// suspendu par un administrateur conserve son email_verified_at et reste donc intouché.
public sealed class EmailVerificationCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailVerificationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync();

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEmailVerificationRepository>();

            var now = DateTime.UtcNow;
            var abandoned = await repository.DeleteAbandonedRegistrationsAsync(now);
            var expiredTokens = await repository.DeleteExpiredAsync(now);

            logger.LogInformation(
                "Inscriptions abandonnées supprimées : {Accounts}, jetons de confirmation expirés : {Tokens}",
                abandoned,
                expiredTokens);
        }
        catch (OperationCanceledException)
        {
            // Arrêt de l'application : rien à signaler.
        }
        catch (Exception exception)
        {
            // Un échec ne doit jamais interrompre la boucle de nettoyage.
            logger.LogError(exception, "Échec du nettoyage des inscriptions abandonnées.");
        }
    }
}
