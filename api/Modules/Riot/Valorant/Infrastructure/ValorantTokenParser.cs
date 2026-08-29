using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

// Le puuid est le sujet du JWT Riot. La signature n'est pas vérifiée : ce jeton n'est pas une
// preuve d'identité pour nous, seulement l'adresse du compte chez Riot — qui refusera de lui-même
// un jeton falsifié.
public sealed class ValorantTokenParser : IValorantTokenParser
{
    public string ExtractPuuid(string accessToken)
    {
        var parts = accessToken.Split('.');

        if (parts.Length < 2)
        {
            throw AppException.Validation("RIOT_TOKEN_MALFORMED", "Le jeton Riot fourni n'est pas un JWT.");
        }

        try
        {
            using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));

            return payload.RootElement.TryGetProperty("sub", out var subject)
                   && subject.GetString() is { Length: > 0 } puuid
                ? puuid
                : throw AppException.Validation(
                    "RIOT_TOKEN_MALFORMED",
                    "Le jeton Riot fourni ne porte pas d'identifiant de joueur.");
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw AppException.Validation("RIOT_TOKEN_MALFORMED", "Le jeton Riot fourni est illisible.");
        }
    }

    // Base64url : alphabet différent, et le remplissage est omis.
    private static byte[] DecodeBase64Url(string value)
    {
        var standard = value.Replace('-', '+').Replace('_', '/');
        var padding = (4 - standard.Length % 4) % 4;

        return Convert.FromBase64String(standard.PadRight(standard.Length + padding, '='));
    }
}
