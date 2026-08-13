using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("auth")]
// Porte HTTP du module : elle délègue chaque action à un use case.
public sealed class AuthController(
    LoginUseCase loginUseCase,
    RefreshSessionUseCase refreshSessionUseCase,
    CreateElectronSessionUseCase createElectronSessionUseCase,
    GetGoogleAuthorizationUrlUseCase getGoogleAuthorizationUrlUseCase,
    CompleteGoogleOAuthLoginUseCase completeGoogleOAuthLoginUseCase,
    RequestPasswordResetUseCase requestPasswordResetUseCase,
    ResetPasswordUseCase resetPasswordUseCase,
    RefreshTokenCookieManager refreshTokenCookieManager,
    IOptions<GoogleOAuthOptions> googleOAuthOptions,
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

    [HttpPost("electron/session")]
    public async Task<IActionResult> CreateElectronSession(CancellationToken cancellationToken)
    {
        // Electron appelle cette route avec l'access token reçu par le deep link tools://auth?token=... .
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            throw ApplicationException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }

        var refreshToken = await createElectronSessionUseCase.Execute(authorization["Bearer ".Length..], cancellationToken);
        refreshTokenCookieManager.Set(Response, refreshToken.Value, refreshToken.ExpiresAt);
        return NoContent();
    }

    [HttpGet("google/url")]
    public ActionResult<GoogleAuthorizationUrlResponse> GetGoogleAuthorizationUrl(
        [FromQuery] string source = "web") =>
        Ok(new GoogleAuthorizationUrlResponse(getGoogleAuthorizationUrlUseCase.Execute(source)));

    [HttpGet("callback/google")]
    public async Task<IActionResult> CompleteGoogleOAuthLogin(
        [FromQuery, Required] string code,
        [FromQuery, Required] string state,
        CancellationToken cancellationToken)
    {
        var result = await completeGoogleOAuthLoginUseCase.Execute(code, state, cancellationToken);
        refreshTokenCookieManager.Set(Response, result.Session.RefreshToken, result.Session.RefreshTokenExpiresAt);

        // Compatibilité temporaire avec le front actuel : il lit l'access token dans query.token.
        var redirectUrl = result.Source == "electron"
            ? $"tools://auth?token={Uri.EscapeDataString(result.Session.AccessToken)}"
            : $"{googleOAuthOptions.Value.FrontendBaseUrl}/auth/callback?token={Uri.EscapeDataString(result.Session.AccessToken)}";
        return Redirect(redirectUrl);
    }

    [HttpPost("password/reset-request")]
    public async Task<IActionResult> RequestPasswordReset(
        PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        await requestPasswordResetUseCase.Execute(request.Email, cancellationToken);

        // Réponse volontairement identique dans tous les cas : elle ne dit jamais
        // si un compte existe, ni s'il dispose d'un mot de passe.
        return Ok(new PasswordResetRequestResponse(
            "RESET_REQUESTED",
            "Si un compte correspondant existe, un email a été envoyé."));
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await resetPasswordUseCase.Execute(request.Token, request.Password, cancellationToken);
        return NoContent();
    }
}

// DTO entrant : ASP.NET applique ces règles avant d'appeler Login.
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

// DTO sortant : seul l'access token est exposé au client.
public sealed record LoginResponse(string AccessToken, string TokenType = "Bearer");
public sealed record GoogleAuthorizationUrlResponse(string Url);

public sealed record PasswordResetRequest([Required, EmailAddress] string Email);
public sealed record PasswordResetRequestResponse(string Status, string Message);
public sealed record ResetPasswordRequest([Required] string Token, [Required] string Password);
