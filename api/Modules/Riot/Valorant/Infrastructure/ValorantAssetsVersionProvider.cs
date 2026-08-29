using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class ValorantAssetsVersionProvider(ValorantAssetsReader assetsReader) : IValorantVersionProvider
{
    private const string FileName = "version.json";
    private const string ClientVersionProperty = "riotClientVersion";

    public async Task<IReadOnlyDictionary<string, object>> GetVersion()
    {
        var data = await assetsReader.ReadDataNode(FileName);

        return data.EnumerateObject().ToDictionary(
            property => property.Name,
            property => ToValue(property.Value));
    }

    public async Task<string> GetRiotClientVersion()
    {
        var data = await assetsReader.ReadDataNode(FileName);

        return data.TryGetProperty(ClientVersionProperty, out var version) && version.GetString() is { Length: > 0 } clientVersion
            ? clientVersion
            : throw AppException.Unavailable(
                "VALORANT_CLIENT_VERSION_UNAVAILABLE",
                "La version du client Riot est absente de version.json.");
    }

    // Le contrat rendu au front est celui du fichier : les valeurs sortent telles quelles.
    private static object ToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => element.ToString()
    };
}
