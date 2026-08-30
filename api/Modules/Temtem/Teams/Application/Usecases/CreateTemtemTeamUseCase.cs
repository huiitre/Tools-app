using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Commands;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

// Crée l'équipe et, si la commande en désigne un, y place un premier Temtem.
//
// Les deux dans la même transaction : c'est ce que fait le bouton « créer une équipe » de la
// popup du catalogue, et une équipe vide restée derrière un ajout raté serait un déchet que
// personne ne nettoierait.
public sealed class CreateTemtemTeamUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITemtemCreatureRepository creatureRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(CreateTemtemTeamCommand command)
    {
        var name = TemtemTeamName.Normalize(command.Name);

        if (await teamRepository.NameIsTaken(CurrentUser.UserId, name))
        {
            throw AppException.Conflict("TEAM_NAME_TAKEN", "Vous avez déjà une équipe portant ce nom.");
        }

        if (command.TemtemId is { } temtemId && !await creatureRepository.Exists(temtemId))
        {
            throw AppException.NotFound("TEMTEM_NOT_FOUND", "Ce Temtem n'existe pas.");
        }

        await using var transaction = await transactionManager.BeginAsync();
        var teamId = await teamRepository.Create(CurrentUser.UserId, name);

        if (command.TemtemId is { } firstMember)
        {
            await teamRepository.AddMember(teamId, firstMember, slot: 1);
        }

        await transaction.CommitAsync();

        // Relue après commit : l'appelant reçoit l'équipe telle qu'elle est en base, membre
        // compris, et n'a pas à la reconstituer de son côté.
        return await teamRepository.FindByIdAndUserId(teamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
