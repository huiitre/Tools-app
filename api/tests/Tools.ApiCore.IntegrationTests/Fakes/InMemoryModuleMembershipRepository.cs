using Tools.ApiCore.Modules.Access.Application.Dto;
using Tools.ApiCore.Modules.Access.Application.Ports;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// Appartenances utilisateur ↔ module en mémoire.
//
// Le dictionnaire ne peut contenir qu'un rôle par paire, ce qui reproduit l'invariant tenu
// par l'adaptateur PostgreSQL : celui-ci supprime avant d'insérer, quoi qu'autorise la table.
public sealed class InMemoryModuleMembershipRepository : IModuleMembershipRepository
{
    private readonly Dictionary<(long ModuleId, long UserId), long> rolesByMembership = [];

    public void Reset() => rolesByMembership.Clear();

    public long? RoleOf(long moduleId, long userId) =>
        rolesByMembership.TryGetValue((moduleId, userId), out var roleId) ? roleId : null;

    public Task<IReadOnlyList<ModuleMemberDto>> FindMembersAsync(long moduleId) =>
        Task.FromResult<IReadOnlyList<ModuleMemberDto>>(rolesByMembership
            .Where(entry => entry.Key.ModuleId == moduleId)
            .Select(entry => new ModuleMemberDto(
                entry.Key.UserId,
                "member@example.com",
                "Membre",
                entry.Value,
                "READ_ONLY"))
            .ToList());

    public Task<bool> HasAccessAsync(long moduleId, long userId) =>
        Task.FromResult(rolesByMembership.ContainsKey((moduleId, userId)));

    public Task GrantAsync(long moduleId, long userId, long roleId)
    {
        rolesByMembership[(moduleId, userId)] = roleId;
        return Task.CompletedTask;
    }

    public Task ChangeRoleAsync(long moduleId, long userId, long roleId)
    {
        rolesByMembership[(moduleId, userId)] = roleId;
        return Task.CompletedTask;
    }

    public Task RevokeAsync(long moduleId, long userId)
    {
        rolesByMembership.Remove((moduleId, userId));
        return Task.CompletedTask;
    }
}
