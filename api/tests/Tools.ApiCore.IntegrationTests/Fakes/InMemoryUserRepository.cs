using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// Remplace l'accès PostgreSQL aux utilisateurs. Seul l'utilisateur 1 existe : les tests
// s'appuient dessus pour distinguer un 404 d'un refus de droit.
public sealed class InMemoryUserRepository : IUserRepository
{
    public const long ExistingUserId = 1;

    public long? LastRoleAssignedTo { get; private set; }
    public long? LastRoleAssigned { get; private set; }

    public Task<UserProfileDto?> FindProfileAsync(long userId) =>
        Task.FromResult<UserProfileDto?>(userId == ExistingUserId
            ? new UserProfileDto(userId, "admin@example.com", "Admin", "HUMAN", true, null, [], [])
            : null);

    public Task<IReadOnlyList<UserAdminDto>> FindAllForAdminAsync() =>
        Task.FromResult<IReadOnlyList<UserAdminDto>>(
        [
            new(ExistingUserId, "admin@example.com", "Admin", true, DateTime.UtcNow, null, [4])
        ]);

    public Task<bool> ExistsAsync(long userId) => Task.FromResult(userId == ExistingUserId);

    public Task ReplaceGlobalRoleAsync(long userId, long roleId)
    {
        LastRoleAssignedTo = userId;
        LastRoleAssigned = roleId;
        return Task.CompletedTask;
    }
}
