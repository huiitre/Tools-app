using Tools.ApiCore.Modules.Auth.Domain;

namespace Tools.ApiCore.Modules.Auth.Application.Ports;

// Port BDD : l'Application décrit les données dont elle a besoin, sans connaître PostgreSQL/Dapper.
public interface IAuthRepository
{
    // Retourne l'utilisateur et son hash BCrypt uniquement pour une connexion PASSWORD.
    Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email);

    // Recharge l'utilisateur lors d'un refresh pour détecter une désactivation entre-temps.
    Task<AuthUser?> FindByIdAsync(long userId);

    // Point d'entrée d'une demande de réinitialisation, où seul l'email est connu.
    Task<AuthUser?> FindByEmailAsync(string email);

    // Ces deux lectures alimentent les claims d'autorisation de l'access token. Les rôles de
    // module sont rendus en liste par module : la table autorise le cumul, et arbitrer lequel
    // l'emporte est une règle d'autorisation, pas une affaire de persistance.
    Task<IReadOnlyList<string>> FindGlobalRolesAsync(long userId);
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> FindModuleRolesAsync(long userId);
}
