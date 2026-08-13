// Port BDD : l'Application décrit les données dont elle a besoin, sans connaître PostgreSQL/Dapper.
public interface IAuthRepository
{
    // Retourne l'utilisateur et son hash BCrypt uniquement pour une connexion PASSWORD.
    Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email, CancellationToken cancellationToken);

    // Recharge l'utilisateur lors d'un refresh pour détecter une désactivation entre-temps.
    Task<AuthUser?> FindByIdAsync(long userId, CancellationToken cancellationToken);

    // Ces deux lectures alimentent les claims d'autorisation de l'access token.
    Task<IReadOnlyList<string>> FindGlobalRolesAsync(long userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId, CancellationToken cancellationToken);
}
