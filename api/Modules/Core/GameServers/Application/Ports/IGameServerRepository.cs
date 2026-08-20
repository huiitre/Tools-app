using Tools.Api.Modules.Core.GameServers.Application.Dto;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports;

// Port d'écriture du flux de manifest. Le statut de poll n'est jamais modifié ici.
public interface IGameServerRepository
{
    Task<GameServerUpsertResult> UpsertAsync(GameServerSyncEntry gameServer);

    // Un scan vide est autoritaire : il supprime donc tous les serveurs encore enregistrés.
    Task<int> DeleteMissingAsync(IReadOnlyCollection<string> slugs);

}
