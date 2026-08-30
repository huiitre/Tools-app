using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

// Retirer un membre laisse sa place vacante : le prochain ajout la rebouche, il n'y a aucune
// renumérotation à faire.
public sealed class RemoveTemtemTeamMemberUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(long teamId, long memberId)
    {
        if (!await teamRepository.ExistsForUser(teamId, CurrentUser.UserId))
        {
            throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
        }

        await using var transaction = await transactionManager.BeginAsync();

        if (!await teamRepository.DeleteMember(teamId, memberId))
        {
            throw AppException.NotFound("TEAM_MEMBER_NOT_FOUND", "Ce membre n'est pas dans cette équipe.");
        }

        await teamRepository.TouchUpdatedAt(teamId);
        await transaction.CommitAsync();

        return await teamRepository.FindByIdAndUserId(teamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
