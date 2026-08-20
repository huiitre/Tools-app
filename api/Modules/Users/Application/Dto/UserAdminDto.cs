namespace Tools.Api.Modules.Users.Application.Dto;

// Ligne du tableau d'administration des utilisateurs.
//
// `RoleId` ne porte qu'un identifiant, pas un objet : le frontend le résout contre le
// catalogue chargé séparément par GET /roles. Nul pour un compte sans rôle global.
public sealed record UserAdminDto(
    long Id,
    string Email,
    string Name,
    bool Active,
    DateTime? CreatedAt,

    // Avatar Google lorsqu'il existe ; le tableau retombe sur les initiales sinon.
    string? AvatarUrl,

    long? RoleId
);
