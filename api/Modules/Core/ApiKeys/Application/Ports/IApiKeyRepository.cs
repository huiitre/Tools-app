using System.Net;
using Tools.Api.Modules.Core.ApiKeys.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Ports;

public interface IApiKeyRepository
{
    Task<List<ApiKey>> FindAll();
    Task<ApiKey?> FindById(long id);

    // Recherche à chaque appel authentifié par clé : c'est le hash qui est indexé.
    Task<ApiKey?> FindByHash(byte[] keyHash);

    // Seules les clés non révoquées retiennent leur nom.
    Task<bool> ActiveNameIsTaken(string name);

    // Insère la clé et ses scopes ; à appeler dans une transaction.
    Task<long> Create(ApiKey apiKey);

    // Faux si la clé n'existe pas ou est déjà révoquée.
    Task<bool> Revoke(long id, DateTimeOffset revokedAt);

    // Hors transaction ; sans effet si la dernière utilisation date de moins de `throttle`.
    Task TouchLastUsed(long id, IPAddress? ip, TimeSpan throttle);
}
