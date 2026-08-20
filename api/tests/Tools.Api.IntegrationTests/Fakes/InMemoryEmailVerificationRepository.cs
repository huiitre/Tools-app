using Tools.Api.Modules.Core.Auth.Application.Ports.Registration;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryEmailVerificationRepository(InMemoryAuthStore store) : IEmailVerificationRepository
{
    public Task SaveAsync(long userId, string token, DateTime expiresAt)
    {
        store.VerificationTokens[token] = (userId, expiresAt);
        return Task.CompletedTask;
    }

    public Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now) =>
        Task.FromResult(store.VerificationTokens.TryGetValue(token, out var entry) && entry.ExpiresAt > now
            ? entry.UserId
            : (long?)null);

    public Task DeleteByUserIdAsync(long userId)
    {
        foreach (var token in store.VerificationTokens
            .Where(entry => entry.Value.UserId == userId)
            .Select(entry => entry.Key)
            .ToList())
        {
            store.VerificationTokens.Remove(token);
        }

        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(DateTime now)
    {
        var expired = store.VerificationTokens
            .Where(entry => entry.Value.ExpiresAt <= now)
            .Select(entry => entry.Key)
            .ToList();

        foreach (var token in expired)
        {
            store.VerificationTokens.Remove(token);
        }

        return Task.FromResult(expired.Count);
    }

    public Task<int> DeleteAbandonedRegistrationsAsync(DateTime now)
    {
        // Reproduit le critère SQL : adresse jamais confirmée et aucun jeton encore valide.
        // is_active n'entre pas dans la décision.
        var abandoned = store.Accounts
            .Where(entry => entry.Value.EmailVerifiedAt is null)
            .Where(entry => !store.VerificationTokens.Values.Any(
                token => token.UserId == entry.Key && token.ExpiresAt > now))
            .Select(entry => entry.Key)
            .ToList();

        foreach (var userId in abandoned)
        {
            store.Accounts.Remove(userId);
            store.Users.Remove(userId);
            store.PasswordHashes.Remove(userId);
            store.Providers.RemoveWhere(provider => provider.UserId == userId);
        }

        return Task.FromResult(abandoned.Count);
    }
}
