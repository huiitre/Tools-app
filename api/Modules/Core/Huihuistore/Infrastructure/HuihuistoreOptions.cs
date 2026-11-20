namespace Tools.Api.Modules.Core.Huihuistore.Infrastructure;

// Ce que l'API de chaque déploiement accepte de publier comme huihuistore. Prod et QA publient deux
// apps distinctes (paquet et clé de signature différents) : une clé d'API qui fuiterait ne permet
// de publier que le vrai huihuistore de cet environnement, signé par sa clé.
//
// Racine de stockage, dossier d'environnement et outils du SDK : ceux de AndroidApps.
public sealed class HuihuistoreOptions
{
    public const string SectionName = "Huihuistore";

    // applicationId attendu, ex. fr.huiitre.huihuistore (prod) ou fr.huiitre.huihuistore.qa.
    public string PackageName { get; init; } = "";

    // SHA-256 du certificat de signature attendu, en hexadécimal. Ce n'est pas un secret : on le lit
    // sur n'importe quel APK publié. Vide : toute publication est refusée.
    public string CertificateSha256 { get; init; } = "";

    // Nombre de versions gardées sur le disque, la version publiée comprise.
    public int KeptReleases { get; init; } = 5;
}
