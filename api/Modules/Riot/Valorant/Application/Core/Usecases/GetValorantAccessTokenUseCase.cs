using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

public sealed class GetValorantAccessTokenUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    ValorantAuthService valorantAuthService
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantTokenView> Execute(long accountId)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(accountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        return new ValorantTokenView(await valorantAuthService.GetOrRefreshAccessToken(accountId));
    }
}
