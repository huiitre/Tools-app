namespace Tools.Api.Modules.Users.Application.Dto;

// Profil de l'utilisateur connecté, tel que le consomme le frontend : `isAdmin` s'appuie sur
// `Role`, `hasModuleAccess` sur `Modules`.
public sealed record UserProfileDto(
    long Id,
    string Email,
    string Name,
    string UserType,
    bool Active,

    // Résolu depuis le provider d'authentification ; nul pour un compte sans avatar.
    string? AvatarUrl,

    // Rôle global, nul si l'utilisateur n'en a aucun. Au plus un : (user_id) est la clé
    // primaire de tools_core.user_role.
    RoleDto? Role,

    // Modules accessibles, chacun avec le rôle que l'utilisateur y détient.
    IReadOnlyList<UserModuleDto> Modules
);
