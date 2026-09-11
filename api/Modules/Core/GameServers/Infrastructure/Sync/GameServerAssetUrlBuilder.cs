using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Sync;

// L'API ne monte pas le filesystem des assets. L'extractor indique donc l'existence d'un fichier
// (image, modpack) par son chemin, et ce builder ne fait que produire son URL publique stable.
public sealed class GameServerAssetUrlBuilder(IOptions<AppOptions> options) : IGameServerAssetUrlBuilder
{
    private readonly string assetsBaseUrl = options.Value.AssetsBaseUrl.TrimEnd('/');

    public string Build(string assetFile) =>
        $"{assetsBaseUrl}/tools_core/gameservers/{string.Join('/', assetFile.Split('/').Select(Uri.EscapeDataString))}";
}
