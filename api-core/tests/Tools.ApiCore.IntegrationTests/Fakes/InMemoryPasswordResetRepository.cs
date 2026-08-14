using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.IntegrationTests.Fakes;

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
