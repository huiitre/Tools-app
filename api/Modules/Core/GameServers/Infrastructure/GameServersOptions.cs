namespace Tools.Api.Modules.Core.GameServers.Infrastructure;

// Paramètres du module. HostOverride n'a de sens qu'en développement (cf. GameServersModule).
public sealed class GameServersOptions
{
    public const string SectionName = "GameServers";

    // Hôte substitué à celui du manifest pour joindre les serveurs depuis un poste de dev.
    public string? HostOverride { get; init; }

    // Interrupteur du poll de fond, pour le couper sur un poste de dev (appsettings.Local.json).
    public bool PollingEnabled { get; init; } = true;

    // Sans elle, les mods hébergés sur CurseForge restent sans icône : son API refuse tout appel
    // anonyme. Secret, jamais commité : appsettings.Local.json en dev, variable
    // GameServers__CurseForgeApiKey sur le conteneur ailleurs.
    public string? CurseForgeApiKey { get; init; }
}
