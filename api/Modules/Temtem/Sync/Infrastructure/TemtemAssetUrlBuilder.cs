using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Temtem.Sync.Infrastructure;

// Les images sont rapatriées sur notre CDN par l'extracteur. Le champ « image » des JSON porte le
// chemin du site source (/img/temtemdex/...) et ne désigne rien chez nous : l'URL se construit
// depuis le nom de fichier, qui n'est pas toujours déductible du code — STATUS donne « statut ».
public sealed class TemtemAssetUrlBuilder(IOptions<AppOptions> options)
{
    private readonly string imagesBaseUrl = $"{options.Value.AssetsBaseUrl.TrimEnd('/')}/tools_temtem/images";

    public string Type(string slug) => $"{imagesBaseUrl}/types/{slug}.png";

    public string Creature(string slug) => $"{imagesBaseUrl}/temtem/{slug}.png";

    public string Category(string fileName) => $"{imagesBaseUrl}/categories/{fileName}.png";

    public string Priority(string fileName) => $"{imagesBaseUrl}/priorities/{fileName}.png";
}
