using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Domain;

namespace Tools.ApiCore.IntegrationTests.Fakes;

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
