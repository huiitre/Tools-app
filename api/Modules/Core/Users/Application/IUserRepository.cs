using Tools.Api.Modules.Core.Users.Application.Dto;

namespace Tools.Api.Modules.Core.Users.Application;

public interface IUserRepository
{
    // Profil complet d'un utilisateur : identité, rôles globaux, modules et rôles par module.
    // Retourne null si l'identifiant ne correspond à aucun compte.
    Task<UserProfileDto?> FindProfileAsync(long userId);

    // Tous les comptes, pour le tableau d'administration.
    Task<IReadOnlyList<UserAdminDto>> FindAllForAdminAsync();

    Task<bool> ExistsAsync(long userId);

    // Remplace le rôle global : les rôles existants sont supprimés avant l'insertion.
    // À appeler dans une transaction — l'opération n'est pas atomique en elle-même.
    Task ReplaceGlobalRoleAsync(long userId, long roleId);

    // Autorise ou refuse la connexion du compte. Une seule écriture, donc atomique en
    // elle-même : contrairement au rôle global, aucune transaction n'est nécessaire.
    Task SetActiveAsync(long userId, bool active);
}
