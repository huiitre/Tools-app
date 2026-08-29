using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

public sealed class RemoveSkinFromWatchlistUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantWatchlistRepository watchlistRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    // Retirer un skin absent n'est pas une erreur : le résultat voulu est déjà obtenu.
    public async Task Execute(long skinId, long accountId)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(accountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        await watchlistRepository.Remove(accountId, skinId);
    }
}
