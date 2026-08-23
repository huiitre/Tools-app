using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class RenameExpeditionUseCase(
    UseCaseAuthorizer authorizer,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    public async Task Execute(Guid expeditionId, RenameExpeditionCommand command)
    {
        if (!await expeditionRepository.ExistsByIdAndUserId(expeditionId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "R2R_EXPEDITION_NOT_FOUND",
                $"L'expédition {expeditionId} est introuvable.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw AppException.Validation("R2R_NAME_REQUIRED", "Le nom de l'expédition est obligatoire.");
        }

        await expeditionRepository.Rename(expeditionId, CurrentUser.UserId, command.Name.Trim());
    }
}
