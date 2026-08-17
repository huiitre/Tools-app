using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Auth.Application.Services;
using Tools.Api.Modules.Common.Application.Exceptions;

namespace Tools.Api.Modules.Auth.Application.Usecases.Session;

// Cas d'usage Electron : convertir un access token reçu via le deep link en cookie refresh dans la session Chromium.
public sealed class CreateElectronSessionUseCase(
    ITokenService tokenService,
    IAuthRepository authRepository)
{
    public async Task<IssuedToken> Execute(string accessToken)
    {
        // L'access token est validé avant toute création de session.
        var accessTokenData = tokenService.ReadAccessToken(accessToken);

        // Le compte est relu pour ne jamais créer une session pour un utilisateur désactivé.
        var user = await authRepository.FindByIdAsync(accessTokenData.UserId);
        if (user is null || !user.IsActive)
        {
            throw AppException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }

        // Electron reçoit son propre cookie refresh : le cookie du navigateur Google externe ne lui appartient pas.
        return tokenService.CreateRefreshToken(user.Id);
    }
}
