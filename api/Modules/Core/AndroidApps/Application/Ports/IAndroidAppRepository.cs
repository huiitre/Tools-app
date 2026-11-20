using Tools.Api.Modules.Core.AndroidApps.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Ports;

// Lectures sur leur propre connexion, écritures dans la transaction ambiante.
public interface IAndroidAppRepository
{
    // Apps et releases, triées par ordre d'affichage puis libellé.
    Task<List<AndroidApp>> FindAll();
    Task<AndroidApp?> FindById(long appId);
    Task<AndroidApp?> FindByPackageName(string packageName);

    // Apps actives ayant une version recommandée, avec cette seule release.
    Task<List<AndroidApp>> FindCatalogue();

    // L'app est placée en dernière position du catalogue.
    Task<long> CreateApp(string packageName, string label);

    Task<long> CreateRelease(long appId, ApkInfo apk, string filePath, string? releaseNotes, long uploadedBy);

    Task UpdateApp(long appId, string label, int displayOrder, bool isActive, long? recommendedReleaseId);

    Task SetRecommendedRelease(long appId, long releaseId);

    Task DeleteRelease(long releaseId);
    Task DeleteApp(long appId);
}
