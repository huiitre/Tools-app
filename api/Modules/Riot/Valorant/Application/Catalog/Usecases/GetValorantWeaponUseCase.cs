using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class GetValorantWeaponUseCase(
    UseCaseAuthorizer authorizer,
    IValorantWeaponRepository weaponRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantWeaponView> Execute(long id)
    {
        var weapon = await weaponRepository.FindById(id);

        return weapon ?? throw AppException.NotFound(
            "VALORANT_WEAPON_NOT_FOUND",
            $"L'arme {id} est introuvable.");
    }
}
