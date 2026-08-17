using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Auth.Domain;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryAuthRepository(InMemoryAuthStore store) : IAuthRepository
{
    public Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email) =>
        Task.FromResult<(AuthUser, string)?>(null);

    public Task<AuthUser?> FindByIdAsync(long userId) =>
        Task.FromResult(store.Users.TryGetValue(userId, out var user) ? user : null);

    public Task<AuthUser?> FindByEmailAsync(string email) =>
        Task.FromResult(store.Users.Values.FirstOrDefault(user => user.Email == email));

    public Task<IReadOnlyList<string>> FindGlobalRolesAsync(long userId) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> FindModuleRolesAsync(long userId) =>
        Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
            new Dictionary<string, IReadOnlyList<string>>());
}
