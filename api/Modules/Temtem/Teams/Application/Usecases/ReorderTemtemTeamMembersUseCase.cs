using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Teams.Application.Commands;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

// Persiste l'ordre produit par le drag-and-drop. Seul le slot change : l'identité du membre — et
// donc ses techniques retenues — reste intacte.
public sealed class ReorderTemtemTeamMembersUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(ReorderTemtemTeamMembersCommand command)
    {
        var team = await teamRepository.FindByIdAndUserId(command.TeamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");

        var currentMemberIds = team.Members.Select(member => member.Id).ToHashSet();
        var orderedMemberIds = command.MemberIds.ToHashSet();

        if (command.MemberIds.Count != team.Members.Count
            || orderedMemberIds.Count != command.MemberIds.Count
            || !orderedMemberIds.SetEquals(currentMemberIds))
        {
            throw AppException.Validation(
                "TEAM_MEMBER_ORDER_INVALID",
                "L'ordre doit contenir une fois chacun des membres de l'équipe.");
        }

        await using var transaction = await transactionManager.BeginAsync();
        await teamRepository.ReorderMembers(command.TeamId, command.MemberIds);
        await teamRepository.TouchUpdatedAt(command.TeamId);
        await transaction.CommitAsync();

        return await teamRepository.FindByIdAndUserId(command.TeamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
