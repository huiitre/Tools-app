using Tools.Api.Modules.Core.AndroidApps.Application.Commands;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-1, second temps : l'APK en attente est relu (rien de l'analyse n'est cru), rangé à sa place
// définitive, puis l'app et la release sont créées. Le fichier est rangé avant l'écriture en
// base : il n'apparaît au catalogue qu'une fois la transaction validée, et il est retiré si elle
// échoue.
public sealed class ConfirmAndroidAppImportUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppFileStore fileStore,
    IApkInspector inspector,
    IAndroidAppRepository repository,
    ITransactionManager transactionManager,
    IAndroidAppAssetUrlBuilder urls
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    private const int ReleaseNotesMaxLength = 2000;

    public async Task<AndroidAppView> Execute(ConfirmAndroidAppImportCommand command, CancellationToken cancellationToken)
    {
        var stagedPath = fileStore.FindStaged(command.ImportId)
            ?? throw AppException.NotFound("ANDROID_APP_IMPORT_NOT_FOUND", "Import introuvable ou expiré.");

        var apk = await inspector.Inspect(stagedPath, cancellationToken);
        var existing = await repository.FindByPackageName(apk.PackageName);

        AndroidAppImportRules.EnsureVersionIsNew(existing, apk);

        var label = existing is null
            ? AndroidApp.NormalizeLabel(command.Label ?? AndroidAppImportRules.DefaultLabel(apk))
            : existing.Label;

        var releaseNotes = NormalizeReleaseNotes(command.ReleaseNotes);
        var filePath = fileStore.Publish(command.ImportId, apk.PackageName, apk.VersionCode);
        long appId;

        try
        {
            await using var transaction = await transactionManager.BeginAsync();

            appId = existing?.Id ?? await repository.CreateApp(apk.PackageName, label);
            var releaseId = await repository.CreateRelease(appId, apk, filePath, releaseNotes, CurrentUser.UserId);

            // Case décochée : la release est importée sans être proposée aux TV (une nightly). Une
            // nouvelle app importée ainsi reste hors du catalogue tant qu'aucune version n'est choisie.
            if (command.Recommend)
            {
                await repository.SetRecommendedRelease(appId, releaseId);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            fileStore.Delete(filePath);
            throw;
        }

        var app = await repository.FindById(appId)
            ?? throw AppException.NotFound("ANDROID_APP_NOT_FOUND", "App introuvable.");

        return AndroidAppView.From(app, urls);
    }

    private static string? NormalizeReleaseNotes(string? releaseNotes)
    {
        var trimmed = releaseNotes?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > ReleaseNotesMaxLength)
        {
            throw AppException.Validation(
                "ANDROID_APP_RELEASE_NOTES_TOO_LONG",
                $"Les notes ne doivent pas dépasser {ReleaseNotesMaxLength} caractères.");
        }

        return trimmed;
    }
}
