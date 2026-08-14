using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Services;
using Tools.ApiCore.Modules.Common.Application.Exceptions;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Session;

// Cas d'usage utilisateur : renouveler l'access token à partir du cookie refresh.
public sealed class RefreshSessionUseCase(
    IAuthRepository authRepository,
    ITokenService tokenService,
    AuthSessionService authSessionService)
{
    public async Task<AuthSession> Execute(string refreshToken, CancellationToken cancellationToken)
    {
        // Vérifie signature, expiration et tokenType=REFRESH ; retourne l'id et la date d'expiration.
        var refreshTokenData = tokenService.ReadRefreshToken(refreshToken);

        // L'utilisateur est relu en BDD : un ancien refresh ne réactive jamais un compte désactivé.
        var user = await authRepository.FindByIdAsync(refreshTokenData.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw AppException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }

        // La date reçue est conservée : le refresh ne rallonge pas la durée maximale de session.
        return await authSessionService.Create(user, refreshTokenData.ExpiresAt, cancellationToken);
    }
}
