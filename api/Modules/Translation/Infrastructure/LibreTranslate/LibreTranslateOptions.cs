namespace Tools.Api.Modules.Translation.Infrastructure.LibreTranslate;

public sealed class LibreTranslateOptions
{
    public const string SectionName = "Translation:LibreTranslate";

    // Conteneur du NAS, sur network_tools ; en dev, le tunnel de dev-console (localhost:5001).
    public string BaseUrl { get; init; } = "http://libretranslate:5000";

    // 5 000 caractères via l'anglais ≈ 16 s, plus l'attente derrière les autres requêtes.
    public int TimeoutSeconds { get; init; } = 60;

    // Seulement si LibreTranslate exige une clé (LT_API_KEYS) ; aucune aujourd'hui.
    public string? ApiKey { get; init; }
}
