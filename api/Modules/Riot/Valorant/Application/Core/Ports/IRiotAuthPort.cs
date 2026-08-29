namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

// Échange un refresh token Riot contre un nouveau couple de jetons. L'échange ne peut pas être
// fait depuis le navigateur (CORS), c'est l'API qui le porte.
public interface IRiotAuthPort
{
    Task<ValorantAuthResponse> Refresh(string refreshToken);

    record ValorantAuthResponse(
        string AccessToken,
        string RefreshToken,
        string Puuid,
        DateTime RefreshTokenExpiresAt
    );
}
