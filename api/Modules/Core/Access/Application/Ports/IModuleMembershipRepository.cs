using Tools.Api.Modules.Core.Access.Application.Dto;

namespace Tools.Api.Modules.Core.Access.Application.Ports;

// Appartenance d'un utilisateur à un module, et rôle qu'il y détient
// (table tools_core.user_module_role).
public interface IModuleMembershipRepository
{
    Task<IReadOnlyList<ModuleMemberDto>> FindMembersAsync(long moduleId);

    Task<bool> HasAccessAsync(long moduleId, long userId);

    // Écritures à appeler dans une transaction : chacune supprime avant d'insérer, pour que
    // la table ne conserve jamais qu'un rôle par paire (utilisateur, module).
    Task GrantAsync(long moduleId, long userId, long roleId);

    Task ChangeRoleAsync(long moduleId, long userId, long roleId);

    Task RevokeAsync(long moduleId, long userId);
}
