using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class ExportExpeditionUseCase(
    UseCaseAuthorizer authorizer,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    // Rend la route brute telle qu'elle a été importée, sans le reste de l'expédition.
    public async Task<string> Execute(Guid expeditionId)
    {
        var routeData = await expeditionRepository.FindRouteDataByIdAndUserId(expeditionId, CurrentUser.UserId);

        return routeData ?? throw AppException.NotFound(
            "R2R_EXPEDITION_NOT_FOUND",
            $"L'expédition {expeditionId} est introuvable.");
    }
}
