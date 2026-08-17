using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Auth.Application.Usecases;
using Tools.Api.Modules.Common.Application.Exceptions;
using Tools.Api.Modules.Auth.Infrastructure.Google;
using Tools.Api.Modules.Auth.Infrastructure.Jwt;
using Tools.Api.Modules.Auth.Application.Usecases.Google;
using Tools.Api.Modules.Auth.Application.Usecases.Password;
using Tools.Api.Modules.Auth.Application.Usecases.Registration;
using Tools.Api.Modules.Auth.Application.Usecases.Session;

namespace Tools.Api.Modules.Auth.Api;

[ApiController]
[Route("auth")]
// Porte HTTP du module : elle délègue chaque action à un use case.
//
// **Les use cases sont résolus par action ([FromServices]), jamais par le constructeur.** C'est
// ici que ça compte le plus : `SetUserPasswordUseCase` est sécurisé, et un use case sécurisé
// applique son contrôle dès sa construction. Injecté au constructeur, il serait construit pour
// chaque requête arrivant sur ce contrôleur — donc `/login`, `/register` et `/refresh`, pourtant
// anonymes, répondraient 401 et plus personne ne pourrait se connecter.
//
// La règle vaut pour tous les use cases de ce fichier, sécurisés ou non : c'est ce qui évite
// qu'ajouter un contrôle à l'un d'eux ferme les routes des autres. Seules les dépendances qui
// n'en sont pas — cookie, options, journal — restent au constructeur.
public sealed class AuthController(
    RefreshTokenCookieManager refreshTokenCookieManager,
    IOptions<GoogleOAuthOptions> googleOAuthOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        [FromServices] LoginUseCase loginUseCase)
    {
        // Le use case vérifie les identifiants et crée les deux tokens.
        var session = await loginUseCase.Execute(request.Email, request.Password);

        // Le refresh token ne sort jamais dans le JSON : il reste dans un cookie HttpOnly.
        refreshTokenCookieManager.Set(Response, session.RefreshToken, session.RefreshTokenExpiresAt);

        // Le front reçoit seulement l'access token à placer dans Authorization: Bearer ...
        return Ok(new LoginResponse(session.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(
        [FromServices] RefreshSessionUseCase refreshSessionUseCase)
    {
        // Le navigateur renvoie normalement ce cookie automatiquement.
        if (!refreshTokenCookieManager.TryGet(Request, out var refreshToken))
        {
            logger.LogDebug("Refresh refusé : cookie refresh_token absent.");
            throw AppException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }

        logger.LogDebug("Refresh demandé avec un cookie refresh_token.");
        // Le use case valide le refresh token, recharge l'utilisateur et crée une nouvelle session.
        var session = await refreshSessionUseCase.Execute(refreshToken);

        // Le cookie est remplacé, tout en gardant sa date d'expiration d'origine.
        refreshTokenCookieManager.Set(Response, session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(new LoginResponse(session.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Un logout consiste ici à supprimer le cookie détenu par le navigateur.
        refreshTokenCookieManager.Clear(Response);
        return NoContent();
    }

    [HttpPost("electron/session")]
    public async Task<IActionResult> CreateElectronSession(
        [FromServices] CreateElectronSessionUseCase createElectronSessionUseCase)
    {
        // Electron appelle cette route avec l'access token reçu par le deep link tools://auth?token=... .
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            throw AppException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }

        var refreshToken = await createElectronSessionUseCase.Execute(authorization["Bearer ".Length..]);
        refreshTokenCookieManager.Set(Response, refreshToken.Value, refreshToken.ExpiresAt);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("google/url")]
    public ActionResult<GoogleAuthorizationUrlResponse> GetGoogleAuthorizationUrl(
        [FromServices] GetGoogleAuthorizationUrlUseCase getGoogleAuthorizationUrlUseCase,
        [FromQuery] string source = "web")
    {
        return Ok(new GoogleAuthorizationUrlResponse(getGoogleAuthorizationUrlUseCase.Execute(source)));
    }

    [AllowAnonymous]
    [HttpGet("callback/google")]
    public async Task<IActionResult> CompleteGoogleOAuthLogin(
        [FromQuery, Required] string code,
        [FromQuery, Required] string state,
        [FromServices] CompleteGoogleOAuthLoginUseCase completeGoogleOAuthLoginUseCase)
    {
        var result = await completeGoogleOAuthLoginUseCase.Execute(code, state);
        refreshTokenCookieManager.Set(Response, result.Session.RefreshToken, result.Session.RefreshTokenExpiresAt);

        // Compatibilité temporaire avec le front actuel : il lit l'access token dans query.token.
        var redirectUrl = result.Source == "electron"
            ? $"tools://auth?token={Uri.EscapeDataString(result.Session.AccessToken)}"
            : $"{googleOAuthOptions.Value.FrontendBaseUrl}/auth/callback?token={Uri.EscapeDataString(result.Session.AccessToken)}";
        return Redirect(redirectUrl);
    }

    [AllowAnonymous]
    [HttpPost("password/reset-request")]
    public async Task<IActionResult> RequestPasswordReset(
        PasswordResetRequest request,
        [FromServices] RequestPasswordResetUseCase requestPasswordResetUseCase)
    {
        await requestPasswordResetUseCase.Execute(request.Email);

        // Réponse volontairement identique dans tous les cas : elle ne dit jamais
        // si un compte existe, ni s'il dispose d'un mot de passe.
        return Ok(new PasswordResetRequestResponse(
            "RESET_REQUESTED",
            "Si un compte correspondant existe, un email a été envoyé."));
    }

    [AllowAnonymous]
    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        [FromServices] ResetPasswordUseCase resetPasswordUseCase)
    {
        await resetPasswordUseCase.Execute(request.Token, request.Password);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        [FromServices] RegisterUserUseCase registerUserUseCase)
    {
        await registerUserUseCase.Execute(
            new RegisterUserCommand(request.Name, request.Email, request.Password));

        // Le compte existe mais reste inactif : seule la confirmation ouvrira la connexion.
        return Ok(new RegisterResponse(
            "VERIFICATION_SENT",
            "Un email de confirmation vient de vous être envoyé."));
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery, Required] string token,
        [FromServices] VerifyEmailUseCase verifyEmailUseCase)
    {
        await verifyEmailUseCase.Execute(token);
        return NoContent();
    }

    // Définir ou changer son propre mot de passe. L'identité vient du jeton, comme pour
    // logout : aucun identifiant n'apparaît dans l'URL, personne ne peut viser un autre compte.
    [HttpPatch("password")]
    public async Task<IActionResult> SetPassword(
        SetPasswordRequest request,
        [FromServices] SetUserPasswordUseCase setUserPasswordUseCase)
    {
        await setUserPasswordUseCase.Execute(new SetUserPasswordCommand(request.Password));
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
public sealed record SetPasswordRequest([Required] string Password);

public sealed record RegisterRequest(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Required] string Password);
public sealed record RegisterResponse(string Status, string Message);
