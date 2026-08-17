using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Auth.Domain;

namespace Tools.Api.Modules.Auth.Application.Services;

// Service partagé : Login et Refresh construisent une session exactement de la même manière.
public sealed class AuthSessionService(IAuthRepository authRepository, ITokenService tokenService)
{
    public async Task<AuthSession> Create(
        AuthUser user,
        DateTimeOffset? refreshTokenExpiresAt)
    {
        // Les droits sont relus en BDD pour être intégrés dans l'access token actuel.
        var roles = await authRepository.FindGlobalRolesAsync(user.Id);
        var modules = await authRepository.FindModuleRolesAsync(user.Id);

        // Au refresh, la date d'expiration existante est fournie pour ne pas prolonger la session indéfiniment.
        var refreshToken = tokenService.CreateRefreshToken(user.Id, refreshTokenExpiresAt);

        // Les deux tokens sont construits avant de remonter au contrôleur.
        return new AuthSession(
            tokenService.CreateAccessToken(user, roles, modules),
            refreshToken.Value,
            refreshToken.ExpiresAt);
    }
}
