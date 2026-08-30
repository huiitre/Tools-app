using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Types.Application.Ports;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Application.Usecases;

public sealed class ListTemtemTypesUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTypeRepository typeRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public Task<List<TemtemTypeView>> Execute()
    {
        return typeRepository.FindAll();
    }
}
