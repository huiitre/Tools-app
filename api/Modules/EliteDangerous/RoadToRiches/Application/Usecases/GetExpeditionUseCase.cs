using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class GetExpeditionUseCase(
    UseCaseAuthorizer authorizer,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    public async Task<ExpeditionDetailView> Execute(Guid expeditionId)
    {
        // La requête filtre déjà sur l'appelant : l'expédition d'un autre est introuvable, pas refusée.
        var expedition = await expeditionRepository.FindByIdAndUserId(expeditionId, CurrentUser.UserId);

        return expedition ?? throw ExpeditionNotFound(expeditionId);
    }

    private static AppException ExpeditionNotFound(Guid expeditionId) =>
        AppException.NotFound("R2R_EXPEDITION_NOT_FOUND", $"L'expédition {expeditionId} est introuvable.");
}
