using System.Net;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.ApiKeys.Domain;

public class ApiKey
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int KeyHashLength = 32;
    public const int MaxLifetimeYears = 3;

    public long? Id { get; }
    public string Name { get; }
    public string Description { get; }
    public byte[] KeyHash { get; }
    public string KeyPrefix { get; }
    public IReadOnlySet<ApiKeyScope> Scopes { get; }

    // Codes stockés que le catalogue ne connaît plus : sans effet, gardés pour être signalés.
    public IReadOnlySet<string> UnknownScopes { get; }
    public DateTimeOffset CreatedAt { get; }
    public long CreatedBy { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? RevokedAt { get; }
    public DateTimeOffset? LastUsedAt { get; }
    public IPAddress? LastUsedIp { get; }

    private ApiKey(
        long? id,
        string name,
        string description,
        byte[] keyHash,
        string keyPrefix,
        IReadOnlySet<ApiKeyScope> scopes,
        IReadOnlySet<string> unknownScopes,
        DateTimeOffset createdAt,
        long createdBy,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt,
        DateTimeOffset? lastUsedAt,
        IPAddress? lastUsedIp)
    {
        // Invariants vrais toute la vie de la clé, vérifiés aussi à la relecture.
        if (id is not null && id <= 0)
            throw AppException.Validation("API_KEY_ID_INVALID", "L'identifiant de la clé est invalide.");

        if (string.IsNullOrWhiteSpace(name))
            throw AppException.Validation("API_KEY_NAME_REQUIRED", "Le nom de la clé est obligatoire.");

        if (name.Length > NameMaxLength)
            throw AppException.Validation("API_KEY_NAME_TOO_LONG", $"Le nom ne doit pas dépasser {NameMaxLength} caractères.");

        if (string.IsNullOrWhiteSpace(description))
            throw AppException.Validation("API_KEY_DESCRIPTION_REQUIRED", "La description de la clé est obligatoire.");

        if (description.Length > DescriptionMaxLength)
            throw AppException.Validation("API_KEY_DESCRIPTION_TOO_LONG", $"La description ne doit pas dépasser {DescriptionMaxLength} caractères.");

        if (keyHash.Length != KeyHashLength)
            throw AppException.Validation("API_KEY_HASH_INVALID", "Le hash de la clé est invalide.");

        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw AppException.Validation("API_KEY_PREFIX_REQUIRED", "Le préfixe de la clé est obligatoire.");

        if (scopes.Count + unknownScopes.Count == 0)
            throw AppException.Validation("API_KEY_SCOPE_REQUIRED", "Au moins un scope est obligatoire.");

        if (createdBy <= 0)
            throw AppException.Validation("API_KEY_CREATOR_INVALID", "Le créateur de la clé est invalide.");

        if (expiresAt <= createdAt)
            throw AppException.Validation("API_KEY_EXPIRATION_BEFORE_CREATION", "La date d'expiration doit être postérieure à la création.");

        if (expiresAt > createdAt.AddYears(MaxLifetimeYears))
            throw AppException.Validation("API_KEY_EXPIRATION_TOO_FAR", $"Une clé ne peut pas vivre plus de {MaxLifetimeYears} ans.");

        Id = id;
        Name = name;
        Description = description;
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        Scopes = scopes;
        UnknownScopes = unknownScopes;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        LastUsedAt = lastUsedAt;
        LastUsedIp = lastUsedIp;
    }

    // Règle propre à la création : une clé relue peut être expirée.
    public static ApiKey Create(
        string name,
        string description,
        byte[] keyHash,
        string keyPrefix,
        IEnumerable<ApiKeyScope> scopes,
        long createdBy,
        DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        if (expiresAt <= now)
            throw AppException.Validation("API_KEY_EXPIRATION_PAST", "La date d'expiration doit être dans le futur.");

        return new ApiKey(
            null, name.Trim(), description.Trim(), keyHash, keyPrefix, scopes.ToHashSet(), new HashSet<string>(),
            now, createdBy, expiresAt, null, null, null);
    }

    // Les codes de scope viennent bruts de la base : ceux que le catalogue ne connaît plus sont isolés.
    public static ApiKey Rehydrate(
        long id,
        string name,
        string description,
        byte[] keyHash,
        string keyPrefix,
        IEnumerable<string> scopeCodes,
        DateTimeOffset createdAt,
        long createdBy,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt,
        DateTimeOffset? lastUsedAt,
        IPAddress? lastUsedIp)
    {
        var scopes = new HashSet<ApiKeyScope>();
        var unknownScopes = new HashSet<string>();

        foreach (var code in scopeCodes)
        {
            if (ApiKeyScopes.Parse(code) is { } scope) scopes.Add(scope);
            else unknownScopes.Add(code);
        }

        return new ApiKey(
            id, name, description, keyHash, keyPrefix, scopes, unknownScopes,
            createdAt, createdBy, expiresAt, revokedAt, lastUsedAt, lastUsedIp);
    }

    public bool HasScope(ApiKeyScope scope) => Scopes.Contains(scope);

    public bool IsValid()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (RevokedAt != null || ExpiresAt <= now) return false;
        return true;
    }
}
