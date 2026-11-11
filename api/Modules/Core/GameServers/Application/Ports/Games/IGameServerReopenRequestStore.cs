namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Dernière demande de réouverture par serveur, gardée en mémoire (Singleton) : un redémarrage de
// l'API l'oublie, au pire une notification de plus. Effacée par le poll au retour en ligne.
public interface IGameServerReopenRequestStore
{
    // Atomique : faux si une demande date de moins de `cooldown`, `requestedAt` étant alors la sienne.
    bool TryRequest(string slug, DateTime now, TimeSpan cooldown, out DateTime requestedAt);

    DateTime? Get(string slug);

    void Clear(string slug);

    // Annule une demande dont la notification n'est pas partie, sans toucher à une plus récente.
    void Cancel(string slug, DateTime requestedAt);
}
