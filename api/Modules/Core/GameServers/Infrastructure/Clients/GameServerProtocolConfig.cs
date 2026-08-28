using System.Text.Json;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

// Lecture du protocol_config, partagée par les adaptateurs de statut et les providers de jeu :
// elle prend le JSON brut plutôt qu'une cible, les deux ne portant pas le même type.
internal static class GameServerProtocolConfig
{
    public static string? GetString(string protocolConfig, string propertyName)
    {
        using var document = JsonDocument.Parse(protocolConfig);
        return document.RootElement.TryGetProperty(propertyName, out var value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static int? GetPositiveInt(string protocolConfig, string propertyName)
    {
        using var document = JsonDocument.Parse(protocolConfig);
        return document.RootElement.TryGetProperty(propertyName, out var value)
               && value.TryGetInt32(out var number)
               && number >= 0
            ? number
            : null;
    }
}
