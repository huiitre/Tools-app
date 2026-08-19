namespace Tools.Api.Modules.GameServers.Application.Ports;

// Le sync lit le fichier consolidé produit par l'extractor. La route interne ne transporte pas
// le tableau : elle ne fait que déclencher ce flux, comme les synchronisations Java existantes.
public interface IGameServersManifestProvider
{
    Task<IReadOnlyList<GameServerSyncDto>> FetchAsync();
}
