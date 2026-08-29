using System.Text.Json;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Riot.Common.Infrastructure;

// Les images sont rapatriées sur le NAS par l'extracteur, sous une arborescence qui reprend les
// identifiants Riot. La base publique n'est donc pas le CDN de Riot mais le nôtre.
//
// Le champ CDN de la source ne sert qu'à savoir si l'image existe : sa valeur, elle, est ignorée.
public sealed class ValorantAssetUrlBuilder(IOptions<AppOptions> options)
{
    private readonly string imagesBaseUrl = $"{options.Value.AssetsBaseUrl.TrimEnd('/')}/tools_riot/valorant/img";

    public string Image(string relativePath) => $"{imagesBaseUrl}/{relativePath}";

    public string? ImageIfPresent(JsonElement element, string cdnPropertyName, string relativePath) =>
        ValorantAssetJson.OptionalString(element, cdnPropertyName) is null ? null : Image(relativePath);
}
