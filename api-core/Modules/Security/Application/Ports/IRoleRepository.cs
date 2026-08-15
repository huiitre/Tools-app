using Tools.ApiCore.Modules.Security.Application.Dto;

namespace Tools.ApiCore.Modules.Security.Application.Ports;

// Catalogue des rôles attribuables.
//
// Il vit dans Security parce que c'est la matérialisation en base de `RoleCode`, et parce que
// deux modules s'en servent : Users pour le rôle global, Access pour le rôle contextuel d'un
// module. Le ranger sous l'un des deux laisserait croire à une appartenance qui n'existe pas.
public interface IRoleRepository
{
    Task<IReadOnlyList<RoleDto>> FindAllAsync();

    // Vrai si l'identifiant correspond à un rôle du catalogue. Les use cases d'attribution
    // s'en servent pour refuser un rôle inexistant avant d'écrire quoi que ce soit.
    Task<bool> ExistsAsync(long roleId);

    // Identifiant du rôle portant ce code, ou null. Sert au rôle par défaut d'un nouvel accès.
    Task<long?> FindIdByCodeAsync(string code);
}
