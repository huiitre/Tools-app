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

// Fixe les techniques retenues pour un membre. Deux règles qu'aucune contrainte SQL ne sait
// exprimer vivent ici : quatre techniques au maximum — un CHECK ne compte pas de lignes — et le
// fait que le Temtem apprenne réellement chacune d'elles, ce que seule `temtem_technique` dit.
public sealed class SetTemtemTeamMemberTechniquesUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository,
    ITemtemCreatureRepository creatureRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemTeamView> Execute(SetTemtemTeamMemberTechniquesCommand command)
    {
        if (!await teamRepository.ExistsForUser(command.TeamId, CurrentUser.UserId))
        {
            throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
        }

        // Un doublon n'est pas un refus : la clé primaire (membre, technique) le rend sans objet,
        // et deux fois la même technique reste un seul choix.
        var techniqueIds = command.TechniqueIds.Distinct().ToList();

        if (techniqueIds.Count > TeamRoster.MaxTechniquesPerMember)
        {
            throw AppException.Validation(
                "TOO_MANY_TECHNIQUES",
                $"Un Temtem ne peut pas retenir plus de {TeamRoster.MaxTechniquesPerMember} techniques.");
        }

        await using var transaction = await transactionManager.BeginAsync();

        var temtemId = await teamRepository.FindMemberTemtemId(command.TeamId, command.MemberId)
            ?? throw AppException.NotFound("TEAM_MEMBER_NOT_FOUND", "Ce membre n'est pas dans cette équipe.");

        var learned = await creatureRepository.FindLearnedTechniqueIds(temtemId);
        var unknown = techniqueIds.Where(id => !learned.Contains(id)).ToList();

        if (unknown.Count > 0)
        {
            throw AppException.Validation(
                "TECHNIQUE_NOT_LEARNABLE",
                $"Ce Temtem n'apprend pas la ou les techniques suivantes : {string.Join(", ", unknown)}.");
        }

        await teamRepository.ReplaceMemberTechniques(command.MemberId, techniqueIds);
        await teamRepository.TouchUpdatedAt(command.TeamId);
        await transaction.CommitAsync();

        return await teamRepository.FindByIdAndUserId(command.TeamId, CurrentUser.UserId)
            ?? throw AppException.NotFound("TEAM_NOT_FOUND", "Équipe introuvable.");
    }
}
