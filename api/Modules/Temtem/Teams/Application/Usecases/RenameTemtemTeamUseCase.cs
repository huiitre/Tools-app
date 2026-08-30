using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Teams.Application.Commands;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

public sealed class RenameTemtemTeamUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(RenameTemtemTeamCommand command)
    {
        var name = TemtemTeamName.Normalize(command.Name);

        // L'équipe renommée est exclue du contrôle : réenregistrer le même nom n'est pas un
        // conflit avec soi-même.
        if (await teamRepository.NameIsTaken(CurrentUser.UserId, name, command.TeamId))
        {
            throw AppException.Conflict("TEAM_NAME_TAKEN", "Vous avez déjà une équipe portant ce nom.");
        }

        await using var transaction = await transactionManager.BeginAsync();

        // Le SQL filtre sur le propriétaire : l'équipe d'un autre est introuvable, pas interdite.
        if (!await teamRepository.Rename(command.TeamId, CurrentUser.UserId, name))
        {
            throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
        }

        await transaction.CommitAsync();

        return await teamRepository.FindByIdAndUserId(command.TeamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
