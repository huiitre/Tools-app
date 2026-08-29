using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

public sealed class GetValorantVersionUseCase(
    UseCaseAuthorizer authorizer,
    IValorantVersionProvider versionProvider
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public Task<IReadOnlyDictionary<string, object>> Execute()
    {
        return versionProvider.GetVersion();
    }
}
