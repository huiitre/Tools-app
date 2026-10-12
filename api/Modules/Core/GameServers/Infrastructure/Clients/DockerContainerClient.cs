using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

// Redémarre un conteneur par l'API Docker, jointe à travers tools_docker_proxy qui n'autorise que
// stop/restart/kill. Docker ne répond qu'une fois le conteneur relancé, jusqu'à StopTimeout plus
// tard : l'appel n'est attendu que le temps de voir un refus immédiat, puis laissé en fond.
public sealed class DockerContainerClient(HttpClient httpClient, ILogger<DockerContainerClient> logger)
{
    // Délai laissé au serveur pour sauvegarder avant que Docker ne le tue.
    public static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(180);
    private static readonly TimeSpan FailFastWindow = TimeSpan.FromSeconds(3);

    public async Task RestartAsync(string container)
    {
        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("La variable DOCKER_API_HOST n'est pas renseignée.");
        }

        var restart = SendRestartAsync(container);
        if (await Task.WhenAny(restart, Task.Delay(FailFastWindow)) == restart)
        {
            await restart;
            return;
        }

        _ = restart.ContinueWith(
            failed => logger.LogError(failed.Exception, "Échec du redémarrage du conteneur {Container}.", container),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    // Jamais annulé : couper la requête en plein arrêt laisserait le conteneur arrêté sans relance.
    private async Task SendRestartAsync(string container)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(
                $"containers/{Uri.EscapeDataString(container)}/restart?t={(int)StopTimeout.TotalSeconds}",
                content: null,
                CancellationToken.None);
        }
        catch (HttpRequestException exception)
        {
            throw AppException.Unavailable("DOCKER_UNREACHABLE", $"L'API Docker est injoignable : {exception.Message}");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw AppException.Unavailable(
                    "DOCKER_RESTART_REFUSED",
                    $"Docker a refusé de redémarrer « {container} » ({(int)response.StatusCode}).");
            }
        }
    }
}
