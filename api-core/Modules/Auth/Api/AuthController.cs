using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("auth")]
// Porte HTTP du module : elle délègue chaque action à un use case.
public sealed class AuthController(
    LoginUseCase loginUseCase,
    RefreshSessionUseCase refreshSessionUseCase,
    RefreshTokenCookieManager refreshTokenCookieManager,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        // Le use case vérifie les identifiants et crée les deux tokens.
        var session = await loginUseCase.Execute(request.Email, request.Password, cancellationToken);

        // Le refresh token ne sort jamais dans le JSON : il reste dans un cookie HttpOnly.
        refreshTokenCookieManager.Set(Response, session.RefreshToken, session.RefreshTokenExpiresAt);

        // Le front reçoit seulement l'access token à placer dans Authorization: Bearer ...
        return Ok(new LoginResponse(session.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        // Le navigateur renvoie normalement ce cookie automatiquement.
        if (!refreshTokenCookieManager.TryGet(Request, out var refreshToken))
        {
            logger.LogDebug("Refresh refusé : cookie refresh_token absent.");
            throw ApplicationException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }

        logger.LogDebug("Refresh demandé avec un cookie refresh_token.");
        // Le use case valide le refresh token, recharge l'utilisateur et crée une nouvelle session.
        var session = await refreshSessionUseCase.Execute(refreshToken, cancellationToken);

        // Le cookie est remplacé, tout en gardant sa date d'expiration d'origine.
        refreshTokenCookieManager.Set(Response, session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(new LoginResponse(session.AccessToken));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Un logout consiste ici à supprimer le cookie détenu par le navigateur.
        refreshTokenCookieManager.Clear(Response);
        return NoContent();
    }
}

// DTO entrant : ASP.NET applique ces règles avant d'appeler Login.
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

// DTO sortant : seul l'access token est exposé au client.
public sealed record LoginResponse(string AccessToken, string TokenType = "Bearer");
