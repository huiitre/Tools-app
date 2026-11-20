using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-1, premier temps : l'APK est mis en attente et lu, rien n'est créé. Les erreurs bloquantes
// (fichier illisible, version déjà importée) arrêtent l'import ici et suppriment le fichier ; le
// reste devient un avertissement que la popup montre avant la confirmation.
public sealed class AnalyzeAndroidAppImportUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppFileStore fileStore,
    IApkInspector inspector,
    IAndroidAppRepository repository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<AndroidAppImportAnalysisView> Execute(Stream content, CancellationToken cancellationToken)
    {
        var importId = await fileStore.Stage(content, cancellationToken);

        try
        {
            var stagedPath = fileStore.FindStaged(importId)
                ?? throw AppException.NotFound("ANDROID_APP_IMPORT_NOT_FOUND", "Import introuvable ou expiré.");

            var apk = await inspector.Inspect(stagedPath, cancellationToken);
            var existing = await repository.FindByPackageName(apk.PackageName);

            AndroidAppImportRules.EnsureVersionIsNew(existing, apk);

            return new AndroidAppImportAnalysisView(
                importId,
                fileStore.StagedExpiresAt(importId),
                ApkInfoView.From(apk),
                existing is null ? null : ExistingView(existing),
                existing?.Label ?? AndroidAppImportRules.DefaultLabel(apk),
                AndroidAppImportRules.Warnings(existing, apk));
        }
        catch
        {
            fileStore.DeleteStaged(importId);
            throw;
        }
    }

    private static AndroidAppImportExistingAppView ExistingView(AndroidApp app)
    {
        var recommended = app.RecommendedRelease;

        return new AndroidAppImportExistingAppView(
            app.Id,
            app.Label,
            recommended?.VersionCode,
            recommended?.VersionName,
            recommended?.MinSdk,
            recommended?.FileSize,
            recommended is null ? null : Convert.ToHexStringLower(recommended.CertSha256));
    }
}
