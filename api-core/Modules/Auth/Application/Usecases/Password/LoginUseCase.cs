using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Services;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Password;

// Cas d'usage utilisateur : se connecter avec email et mot de passe.
public sealed class LoginUseCase(
    IAuthRepository authRepository,
    IPasswordHasher passwordHasher,
    AuthSessionService authSessionService)
{
    public async Task<AuthSession> Execute(string email, string password)
    {
        // Une seule requête récupère l'utilisateur PASSWORD et son hash.
        var candidate = await authRepository.FindPasswordLoginAsync(email);
        var passwordMatches = false;
        if (candidate is not null)
        {
            try
            {
                // BCrypt compare le mot de passe reçu au hash stocké, jamais à un mot de passe en clair.
                passwordMatches = passwordHasher.Matches(password, candidate.Value.PasswordHash);
            }
            catch (ArgumentException)
            {
                // Un hash invalide est traité comme un mauvais mot de passe sans exposer de détail.
                passwordMatches = false;
            }
        }

        if (candidate is null || !passwordMatches)
        {
            // Même réponse pour utilisateur inconnu et mot de passe faux afin de ne pas révéler les comptes existants.
            throw AppException.Unauthorized("INVALID_CREDENTIALS", "Identifiants invalides.");
        }

        if (!candidate.Value.User.IsActive)
        {
            // Un compte désactivé ne peut pas obtenir de nouvelle session.
            throw AppException.Unauthorized("USER_DISABLED", "Utilisateur désactivé.");
        }

        // Le service partagé lit les droits puis crée l'access et le refresh token.
        return await authSessionService.Create(candidate.Value.User, null);
    }
}
