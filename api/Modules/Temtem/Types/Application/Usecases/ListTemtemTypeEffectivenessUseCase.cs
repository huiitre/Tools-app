using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Types.Application.Ports;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Application.Usecases;

// Référentiel brut du simulateur : le front garde ses 144 lignes en mémoire et applique le
// produit sur les doubles types. Il n'y a pas de verdict métier à persister côté API.
public sealed class ListTemtemTypeEffectivenessUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemTypeRepository typeRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<List<TemtemTypeEffectivenessView>> Execute()
    {
        var matrix = await typeRepository.FindEffectivenessMatrix();

        return matrix
            .OrderBy(entry => entry.Key.Attacker)
            .ThenBy(entry => entry.Key.Defender)
            .Select(entry => new TemtemTypeEffectivenessView(
                entry.Key.Attacker,
                entry.Key.Defender,
                entry.Value))
            .ToList();
    }
}
