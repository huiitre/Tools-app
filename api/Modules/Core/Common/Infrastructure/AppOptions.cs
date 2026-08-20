namespace Tools.Api.Modules.Core.Common.Infrastructure;

// Paramètres généraux de l'application, indépendants d'un module.
public sealed class AppOptions
{
    public const string SectionName = "App";

    // Base des liens envoyés par email (réinitialisation de mot de passe, etc.).
    public string FrontendBaseUrl { get; init; } = "http://localhost:5173";

    // CDN public des assets NAS. L'API produit des URLs, sans monter leur filesystem.
    public string AssetsBaseUrl { get; init; } = "https://assets.tools.huiitre.fr";
}
