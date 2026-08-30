using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

public sealed class DeleteTemtemTeamUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    // Membres et techniques retenues partent en cascade : la suppression est une seule requête.
    public async Task Execute(long teamId)
    {
        await using var transaction = await transactionManager.BeginAsync();

        if (!await teamRepository.Delete(teamId, CurrentUser.UserId))
        {
            throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
        }

        await transaction.CommitAsync();
    }
}
