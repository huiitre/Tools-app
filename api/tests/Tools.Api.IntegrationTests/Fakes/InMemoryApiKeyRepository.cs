using System.Net;
using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Domain;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.IntegrationTests.Fakes;

// Clés d'API sans PostgreSQL. `Add` rend la clé en clair, comme la création réelle.
public sealed class InMemoryApiKeyRepository : IApiKeyRepository
{
    private readonly List<ApiKey> apiKeys = [];
    private long nextId = 1;

    public List<(long Id, IPAddress? Ip)> Touches { get; } = [];

    public string Add(
        IEnumerable<ApiKeyScope> scopes,
        bool revoked = false,
        bool expired = false,
        IEnumerable<string>? unknownScopes = null)
    {
        var secret = ApiKeySecret.Generate();
        var now = DateTimeOffset.UtcNow;
        var createdAt = expired ? now.AddYears(-1) : now.AddMinutes(-5);

        apiKeys.Add(ApiKey.Rehydrate(
            nextId++,
            $"Clé de test {nextId}",
            "Clé créée par un test d'intégration",
            secret.Hash,
            secret.Prefix,
            scopes.Select(scope => scope.ToCode()).Concat(unknownScopes ?? []),
            createdAt,
            1,
            expired ? now.AddDays(-1) : now.AddYears(1),
            revoked ? now.AddMinutes(-1) : null,
            null,
            null));

        return secret.Value;
    }

    public void Reset()
    {
        apiKeys.Clear();
        Touches.Clear();
        nextId = 1;
    }

    public Task<List<ApiKey>> FindAll() => Task.FromResult(apiKeys.ToList());

    public Task<ApiKey?> FindById(long id) =>
        Task.FromResult(apiKeys.FirstOrDefault(k => k.Id == id));

    public Task<ApiKey?> FindByHash(byte[] keyHash) =>
        Task.FromResult(apiKeys.FirstOrDefault(k => k.KeyHash.AsSpan().SequenceEqual(keyHash)));

    public Task<bool> ActiveNameIsTaken(string name) =>
        Task.FromResult(apiKeys.Any(k => k.Name == name && k.RevokedAt is null));

    public Task<long> Create(ApiKey apiKey)
    {
        var id = nextId++;
        apiKeys.Add(ApiKey.Rehydrate(
            id, apiKey.Name, apiKey.Description, apiKey.KeyHash, apiKey.KeyPrefix,
            apiKey.Scopes.Select(scope => scope.ToCode()), apiKey.CreatedAt, apiKey.CreatedBy,
            apiKey.ExpiresAt, null, null, null));
        return Task.FromResult(id);
    }

    public Task<bool> Revoke(long id, DateTimeOffset revokedAt)
    {
        var index = apiKeys.FindIndex(k => k.Id == id && k.RevokedAt is null);
        if (index < 0) return Task.FromResult(false);

        var k = apiKeys[index];
        apiKeys[index] = ApiKey.Rehydrate(
            id, k.Name, k.Description, k.KeyHash, k.KeyPrefix,
            k.Scopes.Select(scope => scope.ToCode()), k.CreatedAt, k.CreatedBy, k.ExpiresAt,
            revokedAt, k.LastUsedAt, k.LastUsedIp);
        return Task.FromResult(true);
    }

    public Task TouchLastUsed(long id, IPAddress? ip, TimeSpan throttle)
    {
        Touches.Add((id, ip));
        return Task.CompletedTask;
    }
}
