using Tools.Api.Modules.Core.Users.Application;
using Tools.Api.Modules.Core.Users.Application.Dto;

namespace Tools.Api.IntegrationTests.Fakes;

// Remplace l'accès PostgreSQL aux utilisateurs. Seul l'utilisateur 1 existe : les tests
// s'appuient dessus pour distinguer un 404 d'un refus de droit.
public sealed class InMemoryUserRepository : IUserRepository
{
    public const long ExistingUserId = 1;

    public long? LastRoleAssignedTo { get; private set; }
    public long? LastRoleAssigned { get; private set; }

    public Task<UserProfileDto?> FindProfileAsync(long userId) =>
        Task.FromResult<UserProfileDto?>(userId == ExistingUserId
            ? new UserProfileDto(userId, "admin@example.com", "Admin", "HUMAN", true, null, null, [])
            : null);

    public Task<IReadOnlyList<UserAdminDto>> FindAllForAdminAsync() =>
        Task.FromResult<IReadOnlyList<UserAdminDto>>(
        [
            new(ExistingUserId, "admin@example.com", "Admin", true, DateTime.UtcNow, null, 4)
        ]);

    public Task<bool> ExistsAsync(long userId) => Task.FromResult(userId == ExistingUserId);

    public Task ReplaceGlobalRoleAsync(long userId, long roleId)
    {
        LastRoleAssignedTo = userId;
        LastRoleAssigned = roleId;
        return Task.CompletedTask;
    }
}
