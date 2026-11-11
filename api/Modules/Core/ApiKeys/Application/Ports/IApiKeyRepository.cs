using Tools.Api.Modules.Core.ApiKeys.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Ports;

public interface IApiKeyRepository
{
    Task<List<ApiKey>> FindAll();
    Task<ApiKey?> FindById(long id);

    // Seules les clés non révoquées retiennent leur nom.
    Task<bool> ActiveNameIsTaken(string name);

    // Insère la clé et ses scopes ; à appeler dans une transaction.
    Task<long> Create(ApiKey apiKey);

    // Faux si la clé n'existe pas ou est déjà révoquée.
    Task<bool> Revoke(long id, DateTimeOffset revokedAt);
}
