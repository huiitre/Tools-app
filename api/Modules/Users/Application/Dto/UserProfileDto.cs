namespace Tools.ApiCore.Modules.Users.Application.Dto;

// Profil de l'utilisateur connecté, tel que le consomme le frontend : `isAdmin` s'appuie sur
// `Roles`, `hasModuleAccess` sur `Modules`. Le contrat reproduit celui de l'API Java pour que
// la bascule du front ne demande aucune adaptation.
public sealed record UserProfileDto(
    long Id,
    string Email,
    string Name,
    string UserType,
    bool Active,

    // Résolu depuis le provider d'authentification ; nul pour un compte sans avatar.
    string? AvatarUrl,

    // Rôles globaux. Un utilisateur peut en cumuler : c'est le plus permissif qui décide.
    IReadOnlyList<RoleDto> Roles,

    // Modules accessibles, chacun avec les rôles que l'utilisateur y détient.
    IReadOnlyList<UserModuleDto> Modules
);
