using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Services;

// Détient le refresh token d'un compte lié : il est le seul à le déchiffrer, à le présenter à Riot
// et à ranger celui que Riot renvoie en échange.
//
// Ce n'est pas un use case sécurisé : le renouvellement périodique l'appelle depuis une tâche de
// fond, sans utilisateur authentifié. Ce sont les use cases qui s'en servent qui portent le
// contrôle d'accès.
public sealed class ValorantAuthService(
    IRiotAuthPort riotAuthPort,
    IValorantAuthRepository valorantAuthRepository,
    IValorantTokenCipher tokenCipher)
{
    // Code que l'adaptateur Riot doit poser sur son AppException quand Riot refuse le refresh
    // token lui-même. Il distingue un jeton périmé — le compte est alors délié — d'une panne
    // passagère, qui ne doit rien supprimer.
    public const string TokenInvalidCode = "RIOT_TOKEN_INVALID";

    public async Task<string> GetOrRefreshAccessToken(long accountId)
    {
        var authData = await valorantAuthRepository.FindById(accountId)
            ?? throw AppException.NotFound(
                "RIOT_AUTH_NOT_FOUND",
                "Aucun jeton n'est enregistré pour ce compte Valorant.");

        var refreshToken = tokenCipher.Decrypt(authData.EncryptedRefreshToken, authData.Iv);

        try
        {
            var riotResponse = await riotAuthPort.Refresh(refreshToken);

            // Riot fait tourner le refresh token à chaque échange : ne pas ranger le nouveau
            // rendrait le compte inutilisable au prochain appel.
            var newIv = tokenCipher.GenerateIv();
            var newEncryptedRefresh = tokenCipher.Encrypt(riotResponse.RefreshToken, newIv);

            // Pseudo et libellé restent nuls : l'upsert ne doit pas écraser ce qui est déjà en base.
            await valorantAuthRepository.Save(
                authData.UserId,
                riotResponse.Puuid,
                authData.Region,
                null,
                null,
                null,
                newEncryptedRefresh,
                newIv,
                riotResponse.RefreshTokenExpiresAt);

            return riotResponse.AccessToken;
        }
        catch (AppException exception) when (exception.Code == TokenInvalidCode)
        {
            // Le jeton est mort chez Riot : le garder ne servirait qu'à rejouer l'échec.
            await valorantAuthRepository.DeleteById(accountId);
            throw;
        }
    }

    public Task<long> SaveAuthData(
        long userId,
        string puuid,
        string region,
        string? gameName,
        string? tagLine,
        string? label,
        string refreshToken,
        DateTime expiresAt)
    {
        var iv = tokenCipher.GenerateIv();
        var encryptedRefresh = tokenCipher.Encrypt(refreshToken, iv);

        return valorantAuthRepository.Save(
            userId, puuid, region, gameName, tagLine, label, encryptedRefresh, iv, expiresAt);
    }
}
