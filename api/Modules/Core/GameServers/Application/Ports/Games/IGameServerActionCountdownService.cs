using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Enregistre une action différée. Un nouvel appel pour le même serveur remplace le compte à
// rebours déjà en cours (jamais deux simultanés) : c'est un choix produit, pas une contrainte
// technique — un admin qui se ravise doit pouvoir changer le délai sans être refusé.
public interface IGameServerActionCountdownService
{
    void Schedule(ScheduledGameServerAction scheduled);
}
