using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Même source que les autres assets : AssetsBaseUrl, jamais une URL stockée en base.
public sealed class AndroidAppAssetUrlBuilder(IOptions<AppOptions> options) : IAndroidAppAssetUrlBuilder
{
    public string Build(string relativePath) =>
        $"{options.Value.AssetsBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
}
