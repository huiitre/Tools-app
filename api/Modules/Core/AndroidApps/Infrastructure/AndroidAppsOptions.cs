namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Réglages propres à chaque déploiement : prod, QA et dev partagent le code mais pas les dossiers.
public sealed class AndroidAppsOptions
{
    public const string SectionName = "AndroidApps";

    // Racine de tools_assets telle que l'API la voit : le montage en conteneur, la copie locale en dev.
    public string StorageRoot { get; init; } = "/data/tools_assets";

    // Dossier d'environnement sous tools_core/android_apps : prod, qa ou dev. La QA et la prod
    // partagent tools_assets, c'est ce dossier qui les sépare. Jamais fourni par l'appelant.
    public string Environment { get; init; } = "";

    // Dossier contenant aapt2 et apksigner (build-tools du SDK Android).
    public string BuildToolsPath { get; init; } = "";

    // Zone d'attente des imports non confirmés. Hors de tools_assets : ce dossier est servi
    // publiquement, un APK pas encore validé n'a pas à y être téléchargeable.
    public string StagingPath { get; init; } = Path.Combine(Path.GetTempPath(), "tools-android-imports");
}
