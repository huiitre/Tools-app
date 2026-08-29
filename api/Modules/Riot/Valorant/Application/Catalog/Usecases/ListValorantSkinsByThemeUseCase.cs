using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class ListValorantSkinsByThemeUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantSkinRepository skinRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<List<ValorantSkinView>> Execute(Guid themeUuid, long? accountId)
    {
        // accountId est facultatif, mais s'il est fourni il doit appartenir à l'appelant : sinon
        // « possédé » et « suivi » seraient lus sur le compte d'un autre.
        if (accountId is { } linkedAccountId && !await valorantAuthRepository.ExistsByIdAndUserId(linkedAccountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        return await skinRepository.FindAllByTierUuid(themeUuid, accountId);
    }
}
