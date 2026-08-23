using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class UpdateProgressUseCase(
    UseCaseAuthorizer authorizer,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    public async Task Execute(Guid expeditionId, UpdateProgressCommand command)
    {
        if (!await expeditionRepository.ExistsByIdAndUserId(expeditionId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "R2R_EXPEDITION_NOT_FOUND",
                $"L'expédition {expeditionId} est introuvable.");
        }

        // Corps omis dans la requête : la colonne est NOT NULL, on écrit une liste vide.
        var bodiesDone = command.CurrentBodiesDone ?? [];

        await expeditionRepository.UpdateProgress(
            expeditionId,
            CurrentUser.UserId,
            command.CurrentSystemIndex,
            bodiesDone);
    }
}
