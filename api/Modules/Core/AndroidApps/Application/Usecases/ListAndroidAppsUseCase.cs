using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-2 : toutes les apps, actives ou non, avec toutes leurs releases.
public sealed class ListAndroidAppsUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppRepository repository,
    IAndroidAppAssetUrlBuilder urls
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<List<AndroidAppView>> Execute()
    {
        var apps = await repository.FindAll();

        return apps.Select(app => AndroidAppView.From(app, urls)).ToList();
    }
}
