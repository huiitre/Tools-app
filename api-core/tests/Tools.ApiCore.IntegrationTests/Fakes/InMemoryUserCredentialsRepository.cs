using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.IntegrationTests.Fakes;

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
