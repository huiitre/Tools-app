using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure;

// L'API ne monte pas le filesystem des assets. L'extractor indique donc l'existence du fichier
// par pictureFile, et ce builder ne fait que produire son URL publique stable.
public sealed class GameServerImageUrlBuilder(IOptions<AppOptions> options) : IGameServerImageUrlBuilder
{
    private readonly string assetsBaseUrl = options.Value.AssetsBaseUrl.TrimEnd('/');

    public string Build(string pictureFile) =>
        $"{assetsBaseUrl}/tools_core/gameservers/{string.Join('/', pictureFile.Split('/').Select(Uri.EscapeDataString))}";
}
