using Tools.Api.Modules.Riot.Sync.Application;
using System.Text.Json;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class ValorantAssetsWeaponDataProvider(
    ValorantAssetsReader assetsReader,
    ValorantAssetUrlBuilder urlBuilder) : IValorantWeaponDataProvider
{
    // Riot préfixe ses catégories du nom de l'énumération moteur : « EEquippableCategory::Heavy ».
    private const string CategoryPrefix = "EEquippableCategory::";

    public async Task<List<ValorantWeaponSyncData>> FetchAll()
    {
        var data = await assetsReader.ReadDataNode("weapons.json");
        var weapons = new List<ValorantWeaponSyncData>();

        foreach (var weapon in data.EnumerateArray())
        {
            var assetId = ValorantAssetJson.RequiredUuid(weapon, "uuid");

            weapons.Add(new ValorantWeaponSyncData(
                assetId,
                ValorantAssetJson.OptionalString(weapon, "displayName") ?? string.Empty,
                ValorantAssetJson.OptionalString(weapon, "category")?.Replace(CategoryPrefix, string.Empty),
                ValorantAssetJson.OptionalUuid(weapon, "defaultSkinUuid"),
                urlBuilder.ImageIfPresent(weapon, "displayIcon", $"weapons/{assetId}/displayicon.png")));
        }

        return weapons;
    }
}
