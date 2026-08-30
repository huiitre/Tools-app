using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

public sealed class ListMyTemtemTeamsUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTeamRepository teamRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    // Le propriétaire n'est pas un argument : c'est l'appelant validé, hors de portée du client.
    public Task<List<TemtemTeamView>> Execute()
    {
        return teamRepository.FindAllByUserId(CurrentUser.UserId);
    }
}
