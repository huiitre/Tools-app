using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Application;

public interface IUserRepository
{
    // Profil complet d'un utilisateur : identité, rôles globaux, modules et rôles par module.
    // Retourne null si l'identifiant ne correspond à aucun compte.
    Task<UserProfileDto?> FindProfileAsync(long userId);
}
