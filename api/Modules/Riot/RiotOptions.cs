namespace Tools.Api.Modules.Riot;

public sealed class RiotOptions
{
    public const string SectionName = "Riot";

    // Clé maître AES-256 en Base64. C'est **le même secret** que le TOOLS_ENCRYPTION_KEY de l'API
    // Java : les refresh tokens déjà en base ont été chiffrés avec, une autre clé les rendrait
    // tous illisibles. Laissée vide, elle est reprise de TOOLS_ENCRYPTION_KEY — la variable que
    // tous les environnements fournissent déjà, aucun déploiement n'a donc rien à ajouter.
    public string EncryptionMasterKey { get; set; } = string.Empty;
}
