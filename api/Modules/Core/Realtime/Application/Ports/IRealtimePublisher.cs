namespace Tools.Api.Modules.Core.Realtime.Application.Ports;

public interface IRealtimePublisher
{
    Task PublishAsync(IReadOnlyCollection<long> userIds, string eventType, object payload);

    // Diffusion à toute connexion active, sans notion de destinataire : réservée aux données
    // publiques à tout utilisateur authentifié (ex. l'état live des serveurs de jeu), jamais à
    // une donnée qui dépend de qui regarde.
    Task PublishToAllAsync(string eventType, object payload);
}
