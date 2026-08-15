namespace Tools.ApiCore.Modules.Common.Infrastructure;

// Paramètres généraux de l'application, indépendants d'un module.
public sealed class AppOptions
{
    public const string SectionName = "App";

    // Base des liens envoyés par email (réinitialisation de mot de passe, etc.).
    public string FrontendBaseUrl { get; init; } = "http://localhost:5173";
}
