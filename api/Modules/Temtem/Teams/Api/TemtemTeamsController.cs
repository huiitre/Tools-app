using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Temtem.Teams.Application.Commands;
using Tools.Api.Modules.Temtem.Teams.Application.Usecases;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Api;

// Équipes de combat de l'appelant.
//
// Chaque écriture rend l'équipe entière plutôt qu'un accusé de réception : le front affiche une
// équipe complète après chaque geste, et n'a donc jamais à la recharger ni à deviner l'état
// obtenu.
[ApiController]
[Route("temtem/teams")]
public class TemtemTeamsController : ControllerBase
{
    [HttpGet]
    public Task<List<TemtemTeamView>> List(
        [FromServices] ListMyTemtemTeamsUseCase listMyTemtemTeamsUseCase
    )
    {
        return listMyTemtemTeamsUseCase.Execute();
    }

    [HttpPost]
    public Task<TemtemTeamView> Create(
        [FromServices] CreateTemtemTeamUseCase createTemtemTeamUseCase,
        CreateTemtemTeamRequest request
    )
    {
        return createTemtemTeamUseCase.Execute(
            new CreateTemtemTeamCommand(request.Name, request.TemtemId));
    }

    [HttpPatch("{teamId:long}")]
    public Task<TemtemTeamView> Rename(
        [FromServices] RenameTemtemTeamUseCase renameTemtemTeamUseCase,
        [FromRoute] long teamId,
        RenameTemtemTeamRequest request
    )
    {
        return renameTemtemTeamUseCase.Execute(new RenameTemtemTeamCommand(teamId, request.Name));
    }

    [HttpDelete("{teamId:long}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteTemtemTeamUseCase deleteTemtemTeamUseCase,
        [FromRoute] long teamId
    )
    {
        await deleteTemtemTeamUseCase.Execute(teamId);

        return NoContent();
    }

    [HttpPost("{teamId:long}/members")]
    public Task<TemtemTeamView> AddMember(
        [FromServices] AddTemtemTeamMemberUseCase addTemtemTeamMemberUseCase,
        [FromRoute] long teamId,
        AddTemtemTeamMemberRequest request
    )
    {
        return addTemtemTeamMemberUseCase.Execute(
            new AddTemtemTeamMemberCommand(teamId, request.TemtemId));
    }

    [HttpDelete("{teamId:long}/members/{memberId:long}")]
    public Task<TemtemTeamView> RemoveMember(
        [FromServices] RemoveTemtemTeamMemberUseCase removeTemtemTeamMemberUseCase,
        [FromRoute] long teamId,
        [FromRoute] long memberId
    )
    {
        return removeTemtemTeamMemberUseCase.Execute(teamId, memberId);
    }

    // PUT et non PATCH : la liste envoyée remplace la précédente, elle ne s'y ajoute pas.
    [HttpPut("{teamId:long}/members/{memberId:long}/techniques")]
    public Task<TemtemTeamView> SetMemberTechniques(
        [FromServices] SetTemtemTeamMemberTechniquesUseCase setTemtemTeamMemberTechniquesUseCase,
        [FromRoute] long teamId,
        [FromRoute] long memberId,
        SetTemtemTeamMemberTechniquesRequest request
    )
    {
        return setTemtemTeamMemberTechniquesUseCase.Execute(
            new SetTemtemTeamMemberTechniquesCommand(teamId, memberId, request.TechniqueIds ?? []));
    }

    // Le Temtem est facultatif : la popup du catalogue crée l'équipe et y place la carte d'un
    // seul geste, la page « Mes équipes » crée une équipe vide.
    public sealed record CreateTemtemTeamRequest(string Name, int? TemtemId);

    public sealed record RenameTemtemTeamRequest(string Name);

    public sealed record AddTemtemTeamMemberRequest(int TemtemId);

    public sealed record SetTemtemTeamMemberTechniquesRequest(IReadOnlyList<int>? TechniqueIds);
}
