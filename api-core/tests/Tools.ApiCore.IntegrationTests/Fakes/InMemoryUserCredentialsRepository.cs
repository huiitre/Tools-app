using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.IntegrationTests.Fakes;

public sealed class InMemoryUserCredentialsRepository(InMemoryAuthStore store) : IUserCredentialsRepository
{
    public Task<bool> ExistsAsync(long userId) =>
        Task.FromResult(store.PasswordHashes.ContainsKey(userId));

    public Task InsertAsync(long userId, string passwordHash)
    {
        store.PasswordHashes[userId] = passwordHash;
        return Task.CompletedTask;
    }

    public Task<int> UpdatePasswordAsync(long userId, string passwordHash)
    {
        if (!store.PasswordHashes.ContainsKey(userId))
        {
            return Task.FromResult(0);
        }

        store.PasswordHashes[userId] = passwordHash;
        return Task.FromResult(1);
    }
}
