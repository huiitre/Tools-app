using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

public sealed class ListValorantAccountsUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    // Le propriétaire n'est pas un argument : c'est l'appelant validé.
    public async Task<List<ValorantAccountView>> Execute()
    {
        var accounts = await valorantAuthRepository.FindAllByUserId(CurrentUser.UserId);

        return accounts
            .Select(account => new ValorantAccountView(
                account.Id,
                account.Puuid,
                account.Region,
                account.GameName,
                account.TagLine,
                account.Label))
            .ToList();
    }
}
