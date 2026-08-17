using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tools.Api.Modules.Auth.Infrastructure.Jwt;

// Paramètres cryptographiques partagés par l'émission des jetons (JwtTokenService) et par
// leur validation dans le pipeline (middleware JwtBearer).
//
// Les deux doivent rester d'accord sur la clé, l'algorithme et les règles de validation. Les
// dupliquer serait la garantie qu'ils divergent un jour : un jeton signé en HS512 et validé
// en HS256 est refusé sans que rien n'indique pourquoi.
public static class JwtTokenParameters
{
    public static SymmetricSecurityKey SigningKey(string secret) => new(SecretBytes(secret));

    // JJWT choisit l'algorithme HMAC selon la taille de la clé ; on reproduit ce comportement
    // pour que l'API Java et le Core acceptent les jetons émis par l'autre.
    public static string Algorithm(string secret) => SecretBytes(secret).Length switch
    {
        >= 64 => SecurityAlgorithms.HmacSha512,
        >= 48 => SecurityAlgorithms.HmacSha384,
        _ => SecurityAlgorithms.HmacSha256
    };

    public static TokenValidationParameters Validation(JwtOptions options, string secret) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(secret),
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = false,
        ValidateLifetime = true,

        // Aucune tolérance sur l'horloge : la fenêtre de révocation vaut la durée de vie de
        // l'access token, une marge par défaut de cinq minutes la doublerait presque.
        ClockSkew = TimeSpan.Zero,
        ValidAlgorithms = [Algorithm(secret)]
    };

    private static byte[] SecretBytes(string secret)
    {
        // HS256 requiert au minimum 32 octets ; une clé insuffisante est une erreur de configuration.
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        return secretBytes.Length >= 32
            ? secretBytes
            : throw new InvalidOperationException("JWT_SECRET doit contenir au moins 32 octets UTF-8.");
    }
}
