using Tools.Api.Modules.Riot.Sync.Application;
using System.Text.Json;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class ValorantAssetsContentTierDataProvider(
    ValorantAssetsReader assetsReader,
    ValorantAssetUrlBuilder urlBuilder) : IValorantContentTierDataProvider
{
    public async Task<List<ValorantContentTierSyncData>> FetchAll()
    {
        var data = await assetsReader.ReadDataNode("contenttiers.json");
        var tiers = new List<ValorantContentTierSyncData>();

        foreach (var tier in data.EnumerateArray())
        {
            var assetId = ValorantAssetJson.RequiredUuid(tier, "uuid");

            tiers.Add(new ValorantContentTierSyncData(
                assetId,
                ValorantAssetJson.OptionalString(tier, "displayName") ?? string.Empty,
                ValorantAssetJson.OptionalString(tier, "devName") ?? string.Empty,
                ValorantAssetJson.Int32(tier, "rank"),
                ValorantAssetJson.Int32(tier, "juiceValue"),
                ValorantAssetJson.Int32(tier, "juiceCost"),
                ValorantAssetJson.OptionalString(tier, "highlightColor"),
                // Les raretés ont toutes leur icône : l'URL est construite sans condition.
                urlBuilder.Image($"contenttiers/{assetId}/displayicon.png")));
        }

        return tiers;
    }
}
