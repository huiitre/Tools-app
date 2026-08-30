using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Application.Usecases;

// Le catalogue entier en un appel : 165 Temtem, aucune pagination. La recherche et les filtres
// sont l'affaire de la grille, côté navigateur, comme sur la Paldex.
public sealed class ListTemtemCreaturesUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemCreatureRepository creatureRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public Task<List<TemtemSummaryView>> Execute()
    {
        return creatureRepository.FindAll();
    }
}
