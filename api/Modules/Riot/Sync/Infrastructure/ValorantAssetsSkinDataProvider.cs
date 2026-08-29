using Tools.Api.Modules.Riot.Sync.Application;
using System.Text.Json;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

// Les skins vivent dans weapons.json, imbriqués sous leur arme : c'est de là que vient leur
// rattachement, aucune autre source ne le donne.
public sealed class ValorantAssetsSkinDataProvider(
    ValorantAssetsReader assetsReader,
    ValorantAssetUrlBuilder urlBuilder) : IValorantSkinDataProvider
{
    public async Task<List<ValorantSkinSyncData>> FetchAll()
    {
        var data = await assetsReader.ReadDataNode("weapons.json");
        var skins = new List<ValorantSkinSyncData>();

        foreach (var weapon in data.EnumerateArray())
        {
            var weaponAssetId = ValorantAssetJson.RequiredUuid(weapon, "uuid");

            foreach (var skin in ValorantAssetJson.Array(weapon, "skins"))
            {
                var assetId = ValorantAssetJson.RequiredUuid(skin, "uuid");
                var levels = ParseLevels(skin);

                skins.Add(new ValorantSkinSyncData(
                    assetId,
                    ValorantAssetJson.OptionalString(skin, "displayName") ?? string.Empty,
                    ResolveIconUrl(assetId, skin, levels),
                    ValorantAssetJson.OptionalUuid(skin, "themeUuid"),
                    ValorantAssetJson.OptionalUuid(skin, "contentTierUuid"),
                    weaponAssetId,
                    levels,
                    ParseChromas(skin)));
            }
        }

        return skins;
    }

    private List<ValorantSkinLevelSyncData> ParseLevels(JsonElement skin)
    {
        // L'index vient de la position dans le tableau, pas d'un champ : Riot n'en publie pas.
        return ValorantAssetJson.Array(skin, "levels")
            .Select((level, index) =>
            {
                var levelAssetId = ValorantAssetJson.RequiredUuid(level, "uuid");

                return new ValorantSkinLevelSyncData(
                    levelAssetId,
                    index,
                    ValorantAssetJson.OptionalString(level, "displayName") ?? string.Empty,
                    ValorantAssetJson.OptionalString(level, "levelItem"),
                    urlBuilder.ImageIfPresent(level, "displayIcon", $"weaponskinlevels/{levelAssetId}/displayicon.png"),
                    // La vidéo reste sur le CDN de Riot : elle n'est pas rapatriée.
                    ValorantAssetJson.OptionalString(level, "streamedVideo"));
            })
            .ToList();
    }

    private List<ValorantSkinChromaSyncData> ParseChromas(JsonElement skin)
    {
        return ValorantAssetJson.Array(skin, "chromas")
            .Select((chroma, index) =>
            {
                var chromaAssetId = ValorantAssetJson.RequiredUuid(chroma, "uuid");

                return new ValorantSkinChromaSyncData(
                    chromaAssetId,
                    index,
                    ValorantAssetJson.OptionalString(chroma, "displayName") ?? string.Empty,
                    urlBuilder.ImageIfPresent(chroma, "displayIcon", $"weaponskinchromas/{chromaAssetId}/displayicon.png"),
                    urlBuilder.ImageIfPresent(chroma, "fullRender", $"weaponskinchromas/{chromaAssetId}/fullrender.png"),
                    urlBuilder.ImageIfPresent(chroma, "swatch", $"weaponskinchromas/{chromaAssetId}/swatch.png"),
                    ValorantAssetJson.OptionalString(chroma, "streamedVideo"));
            })
            .ToList();
    }

    // Beaucoup de skins n'ont pas d'icône propre : celle de leur premier niveau la remplace.
    private string? ResolveIconUrl(Guid assetId, JsonElement skin, List<ValorantSkinLevelSyncData> levels)
    {
        return urlBuilder.ImageIfPresent(skin, "displayIcon", $"weaponskins/{assetId}/displayicon.png")
               ?? levels.Select(level => level.DisplayIconUrl).FirstOrDefault(url => url is not null);
    }
}
