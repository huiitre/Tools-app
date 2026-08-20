using Tools.Api.Modules.Auth.Domain;

namespace Tools.Api.Modules.Auth.Application.Ports;

// Port BDD : l'Application décrit les données dont elle a besoin, sans connaître PostgreSQL/Dapper.
public interface IAuthRepository
{
    // Retourne l'utilisateur et son hash BCrypt uniquement pour une connexion PASSWORD.
    Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email);

    // Recharge l'utilisateur lors d'un refresh pour détecter une désactivation entre-temps.
    Task<AuthUser?> FindByIdAsync(long userId);

    // Point d'entrée d'une demande de réinitialisation, où seul l'email est connu.
    Task<AuthUser?> FindByEmailAsync(string email);

    // Ces deux lectures alimentent les claims d'autorisation de l'access token. Chacune rend
    // une valeur unique : `user_role` a pour clé primaire (user_id) et `user_module_role`
    // (user_id, module_id), il n'y a donc jamais qu'un rôle à lire.
    Task<string?> FindGlobalRoleAsync(long userId);
    Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId);
}
