using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-5 : l'app, ses releases (cascade en base) et tous leurs fichiers. Le front a demandé une
// confirmation explicite ; pour retirer l'app du catalogue sans rien perdre, on la désactive.
public sealed class DeleteAndroidAppUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppRepository repository,
    IAndroidAppFileStore fileStore,
    ITransactionManager transactionManager,
    ILogger<DeleteAndroidAppUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(long appId)
    {
        var app = await repository.FindById(appId)
            ?? throw AppException.NotFound("ANDROID_APP_NOT_FOUND", "App introuvable.");

        await using (var transaction = await transactionManager.BeginAsync())
        {
            await repository.DeleteApp(appId);
            await transaction.CommitAsync();
        }

        try
        {
            foreach (var release in app.Releases)
            {
                fileStore.Delete(release.FilePath);
            }

            fileStore.DeleteAppFolderIfEmpty(app.PackageName);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "App {PackageName} supprimée de la base, fichiers incomplètement supprimés", app.PackageName);
        }
    }
}
