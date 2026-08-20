using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Ports.Password;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryUserAuthProviderRepository(InMemoryAuthStore store) : IUserAuthProviderRepository
{
    public Task<bool> ExistsAsync(long userId, string provider) =>
        Task.FromResult(store.Providers.Contains((userId, provider)));

    public Task InsertAsync(long userId, string provider, string providerUserId, string? providerEmail)
    {
        store.Providers.Add((userId, provider));
        return Task.CompletedTask;
    }
}
