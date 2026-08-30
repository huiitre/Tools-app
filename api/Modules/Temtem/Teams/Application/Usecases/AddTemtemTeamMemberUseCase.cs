using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Commands;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;
using Tools.Api.Modules.Temtem.Teams.Domain;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

// Place un Temtem à la première place libre de l'équipe.
//
// Le même Temtem peut occuper deux places : le jeu l'autorise, et l'interdire ici reviendrait à
// décider à la place du joueur.
public sealed class AddTemtemTeamMemberUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITemtemCreatureRepository creatureRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(AddTemtemTeamMemberCommand command)
    {
        if (!await teamRepository.ExistsForUser(command.TeamId, CurrentUser.UserId))
        {
            throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
        }

        if (!await creatureRepository.Exists(command.TemtemId))
        {
            throw AppException.NotFound("TEMTEM_NOT_FOUND", "Ce Temtem n'existe pas.");
        }

        // Les places occupées sont lues dans la transaction qui insère : deux ajouts simultanés
        // ne peuvent pas viser la même, et l'unicité (team_id, slot) reste le dernier rempart.
        await using var transaction = await transactionManager.BeginAsync();

        var occupiedSlots = await teamRepository.FindOccupiedSlots(command.TeamId);
        var slot = command.Slot switch
        {
            < 1 or > TeamRoster.MaxMembers => throw AppException.Validation(
                "TEAM_SLOT_INVALID",
                $"La place doit être comprise entre 1 et {TeamRoster.MaxMembers}."),
            { } requestedSlot when occupiedSlots.Contains(requestedSlot) => throw AppException.Conflict(
                "TEAM_SLOT_OCCUPIED",
                "Cette place est déjà occupée."),
            { } requestedSlot => requestedSlot,
            null => TeamRoster.FirstFreeSlot(occupiedSlots)
                ?? throw AppException.Conflict(
                    "TEAM_FULL",
                    $"Une équipe ne peut pas dépasser {TeamRoster.MaxMembers} Temtem.")
        };

        await teamRepository.AddMember(command.TeamId, command.TemtemId, slot);
        await teamRepository.TouchUpdatedAt(command.TeamId);
        await transaction.CommitAsync();

        return await teamRepository.FindByIdAndUserId(command.TeamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
