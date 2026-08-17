namespace Tools.ApiCore.Modules.Users.Application.Dto;

// Ligne du tableau d'administration des utilisateurs.
//
// `Roles` ne porte que des identifiants, pas des objets : le frontend les résout contre le
// catalogue chargé séparément par GET /roles. Le contrat reproduit celui de l'API Java pour
// que la bascule ne demande aucune adaptation du tableau.
public sealed record UserAdminDto(
    long Id,
    string Email,
    string Name,
    bool Active,
    DateTime? CreatedAt,

    // Avatar Google lorsqu'il existe ; le tableau retombe sur les initiales sinon.
    string? AvatarUrl,

    IReadOnlyList<long> Roles
);
