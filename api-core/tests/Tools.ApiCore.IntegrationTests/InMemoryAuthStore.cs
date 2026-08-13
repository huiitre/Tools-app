// Doubles mémoire des ports d'accès aux données du module Auth.
// Ils permettent de tester les flux de mot de passe sans PostgreSQL.
public sealed class InMemoryAuthStore
{
    public Dictionary<long, AuthUser> Users { get; } = [];

    // Clé : (userId, provider)
    public HashSet<(long UserId, string Provider)> Providers { get; } = [];

    public Dictionary<long, string> PasswordHashes { get; } = [];

    // Clé : jeton
    public Dictionary<string, (long UserId, DateTime ExpiresAt)> ResetTokens { get; } = [];

    public void Reset()
    {
        Users.Clear();
        Providers.Clear();
        PasswordHashes.Clear();
        ResetTokens.Clear();
    }

    public AuthUser AddUser(long id, string email, bool withPasswordProvider)
    {
        var user = new AuthUser(id, email, true, "HUMAN");
        Users[id] = user;
        if (withPasswordProvider)
        {
            Providers.Add((id, "PASSWORD"));
            PasswordHashes[id] = "hash-existant";
        }

        return user;
    }
}

public sealed class InMemoryAuthRepository(InMemoryAuthStore store) : IAuthRepository
{
    public Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult<(AuthUser, string)?>(null);

    public Task<AuthUser?> FindByIdAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult(store.Users.TryGetValue(userId, out var user) ? user : null);

    public Task<AuthUser?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(store.Users.Values.FirstOrDefault(user => user.Email == email));

    public Task<IReadOnlyList<string>> FindGlobalRolesAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
}

public sealed class InMemoryUserAuthProviderRepository(InMemoryAuthStore store) : IUserAuthProviderRepository
{
    public Task<bool> ExistsAsync(long userId, string provider, CancellationToken cancellationToken) =>
        Task.FromResult(store.Providers.Contains((userId, provider)));

    public Task InsertAsync(long userId, string provider, string providerUserId, string? providerEmail, CancellationToken cancellationToken)
    {
        store.Providers.Add((userId, provider));
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUserCredentialsRepository(InMemoryAuthStore store) : IUserCredentialsRepository
{
    public Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult(store.PasswordHashes.ContainsKey(userId));

    public Task InsertAsync(long userId, string passwordHash, CancellationToken cancellationToken)
    {
        store.PasswordHashes[userId] = passwordHash;
        return Task.CompletedTask;
    }

    public Task<int> UpdatePasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken)
    {
        if (!store.PasswordHashes.ContainsKey(userId))
        {
            return Task.FromResult(0);
        }

        store.PasswordHashes[userId] = passwordHash;
        return Task.FromResult(1);
    }
}

public sealed class InMemoryPasswordResetRepository(InMemoryAuthStore store) : IPasswordResetRepository
{
    public Task SaveAsync(long userId, string token, DateTime expiresAt, CancellationToken cancellationToken)
    {
        store.ResetTokens[token] = (userId, expiresAt);
        return Task.CompletedTask;
    }

    public Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(store.ResetTokens.TryGetValue(token, out var entry) && entry.ExpiresAt > now
            ? entry.UserId
            : (long?)null);

    public Task DeleteByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        foreach (var token in store.ResetTokens.Where(entry => entry.Value.UserId == userId).Select(entry => entry.Key).ToList())
        {
            store.ResetTokens.Remove(token);
        }

        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken)
    {
        var expired = store.ResetTokens.Where(entry => entry.Value.ExpiresAt <= now).Select(entry => entry.Key).ToList();
        foreach (var token in expired)
        {
            store.ResetTokens.Remove(token);
        }

        return Task.FromResult(expired.Count);
    }
}

// Transaction sans effet : les doubles mémoire n'ont rien à valider ni à annuler.
public sealed class NoOpTransactionManager : ITransactionManager
{
    public Task<ITransaction> BeginAsync() => Task.FromResult<ITransaction>(new NoOpTransaction());

    private sealed class NoOpTransaction : ITransaction
    {
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
