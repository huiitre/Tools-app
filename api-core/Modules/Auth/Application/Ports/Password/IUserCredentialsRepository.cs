namespace Tools.ApiCore.Modules.Auth.Application.Ports.Password;

// Port des secrets d'authentification locaux (tools_core.user_credentials).
public interface IUserCredentialsRepository
{
    Task<bool> ExistsAsync(long userId);

    Task InsertAsync(long userId, string passwordHash);

    // Retourne le nombre de lignes modifiées : zéro signifie qu'aucun mot de passe n'existait.
    Task<int> UpdatePasswordAsync(long userId, string passwordHash);
}
