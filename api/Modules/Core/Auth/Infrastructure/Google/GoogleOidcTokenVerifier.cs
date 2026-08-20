using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Tools.Api.Modules.Core.Auth.Application;
using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;

namespace Tools.Api.Modules.Core.Auth.Infrastructure.Google;

// Vérifie les ID tokens Google avec les clés publiques OIDC récupérées automatiquement chez Google.
public sealed class GoogleOidcTokenVerifier(IConfiguration configuration) : IGoogleIdentityVerifier
{
    private const string MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
    private readonly string clientId = configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
        new ConfigurationManager<OpenIdConnectConfiguration>(MetadataAddress, new OpenIdConnectConfigurationRetriever());

    public async Task<GoogleIdentity> VerifyAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("GOOGLE_CLIENT_ID est manquant.");
        }

        try
        {
            // L'API de la bibliothèque exige un jeton d'annulation : on lui passe l'absence de jeton.
            var oidcConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None);
            var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = tokenHandler.ValidateToken(idToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfiguration.SigningKeys,
                ValidateIssuer = true,
                ValidIssuer = "https://accounts.google.com",
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var providerUserId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var name = principal.FindFirst("name")?.Value;
            var picture = principal.FindFirst("picture")?.Value;
            if (string.IsNullOrWhiteSpace(providerUserId)
                || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(name))
            {
                throw AppException.Unauthorized("GOOGLE_AUTHENTICATION_FAILED", "Authentification Google invalide.");
            }

            return new GoogleIdentity(providerUserId, email, name, picture);
        }
        catch (SecurityTokenException)
        {
            throw AppException.Unauthorized("GOOGLE_AUTHENTICATION_FAILED", "Authentification Google invalide.");
        }
        catch (ArgumentException)
        {
            throw AppException.Unauthorized("GOOGLE_AUTHENTICATION_FAILED", "Authentification Google invalide.");
        }
    }
}
