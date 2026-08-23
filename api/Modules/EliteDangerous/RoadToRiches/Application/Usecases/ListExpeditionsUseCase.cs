using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class ListExpeditionsUseCase(
    UseCaseAuthorizer authorizer,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    // Le propriétaire n'est pas un argument : c'est l'appelant validé, hors de portée du client.
    public Task<List<ExpeditionSummaryView>> Execute()
    {
        return expeditionRepository.FindAllByUserId(CurrentUser.UserId);
    }
}
