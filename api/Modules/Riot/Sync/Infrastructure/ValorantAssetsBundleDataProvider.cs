using Tools.Api.Modules.Riot.Sync.Application;
using System.Text.Json;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class ValorantAssetsBundleDataProvider(
    ValorantAssetsReader assetsReader,
    ValorantAssetUrlBuilder urlBuilder) : IValorantBundleDataProvider
{
    public async Task<List<ValorantBundleSyncData>> FetchAll()
    {
        var data = await assetsReader.ReadDataNode("bundles.json");
        var bundles = new List<ValorantBundleSyncData>();

        foreach (var bundle in data.EnumerateArray())
        {
            var assetId = ValorantAssetJson.RequiredUuid(bundle, "uuid");

            bundles.Add(new ValorantBundleSyncData(
                assetId,
                ValorantAssetJson.OptionalString(bundle, "displayName") ?? string.Empty,
                // La bannière n'est rapatriée sur le NAS que si Riot en publie une.
                urlBuilder.ImageIfPresent(bundle, "displayIcon2", $"bundles/{assetId}/displayicon2.png")));
        }

        return bundles;
    }
}
