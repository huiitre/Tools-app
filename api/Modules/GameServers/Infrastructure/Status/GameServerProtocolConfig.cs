using System.Text.Json;
using Tools.Api.Modules.GameServers.Application.Dto;

namespace Tools.Api.Modules.GameServers.Infrastructure.Status;

internal static class GameServerProtocolConfig
{
    public static string? GetString(GameServerPollTarget gameServer, string propertyName)
    {
        using var document = JsonDocument.Parse(gameServer.ProtocolConfig);
        return document.RootElement.TryGetProperty(propertyName, out var value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static int? GetPositiveInt(GameServerPollTarget gameServer, string propertyName)
    {
        using var document = JsonDocument.Parse(gameServer.ProtocolConfig);
        return document.RootElement.TryGetProperty(propertyName, out var value)
               && value.TryGetInt32(out var number)
               && number >= 0
            ? number
            : null;
    }
}
