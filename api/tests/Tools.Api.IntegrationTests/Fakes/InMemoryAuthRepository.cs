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

    public Task<string?> FindGlobalRoleAsync(long userId) =>
        Task.FromResult<string?>(null);

    public Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
}
