using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Tools.ApiCore.Modules.Auth.Domain;

namespace Tools.ApiCore.IntegrationTests.Fakes;

public sealed class InMemoryRegistrationRepository(InMemoryAuthStore store) : IRegistrationRepository
{
    public Task<RegisteredAccount?> FindAccountByEmailAsync(string email)
    {
        foreach (var (id, account) in store.Accounts)
        {
            if (account.Email == email)
            {
                return Task.FromResult<RegisteredAccount?>(
                    new RegisteredAccount(id, account.Email, account.IsActive, account.EmailVerifiedAt));
            }
        }

        return Task.FromResult<RegisteredAccount?>(null);
    }

    public Task<long> CreatePendingUserAsync(string name, string email, string passwordHash)
    {
        var userId = store.Accounts.Count == 0 ? 1 : store.Accounts.Keys.Max() + 1;

        // Compte inactif et adresse non confirmée, comme en base.
        store.Accounts[userId] = (name, email, false, null);
        store.Users[userId] = new AuthUser(userId, email, false, "HUMAN");
        store.PasswordHashes[userId] = passwordHash;
        store.Providers.Add((userId, "PASSWORD"));

        return Task.FromResult(userId);
    }

    public Task ReplacePendingPasswordAsync(long userId, string passwordHash)
    {
        store.PasswordHashes[userId] = passwordHash;
        return Task.CompletedTask;
    }

    public Task MarkEmailVerifiedAsync(long userId, DateTime verifiedAt)
    {
        var account = store.Accounts[userId];
        store.Accounts[userId] = (account.Name, account.Email, true, verifiedAt);
        store.Users[userId] = new AuthUser(userId, account.Email, true, "HUMAN");
        return Task.CompletedTask;
    }

    public Task<string?> FindEmailByIdAsync(long userId) =>
        Task.FromResult(store.Accounts.TryGetValue(userId, out var account) ? account.Email : null);
}
