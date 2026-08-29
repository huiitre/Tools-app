using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class GetValorantWeaponSkinsUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantWeaponRepository weaponRepository,
    IValorantSkinRepository skinRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<List<ValorantSkinView>> Execute(long weaponId, long? accountId)
    {
        // L'arme est vérifiée d'abord : une arme inconnue doit répondre 404, pas une liste vide.
        if (await weaponRepository.FindById(weaponId) is null)
        {
            throw AppException.NotFound(
                "VALORANT_WEAPON_NOT_FOUND",
                $"L'arme {weaponId} est introuvable.");
        }

        if (accountId is { } linkedAccountId && !await valorantAuthRepository.ExistsByIdAndUserId(linkedAccountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        return await skinRepository.FindAllByWeaponId(weaponId, accountId);
    }
}
