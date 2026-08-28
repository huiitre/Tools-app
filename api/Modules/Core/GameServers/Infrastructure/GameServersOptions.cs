namespace Tools.Api.Modules.Core.GameServers.Infrastructure;

// Paramètres du module. HostOverride n'a de sens qu'en développement (cf. GameServersModule).
public sealed class GameServersOptions
{
    public const string SectionName = "GameServers";

    // Hôte substitué à celui du manifest pour joindre les serveurs depuis un poste de dev.
    public string? HostOverride { get; init; }
}
