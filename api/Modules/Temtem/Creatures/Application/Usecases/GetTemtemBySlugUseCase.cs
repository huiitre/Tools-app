using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Application.Usecases;

// La fiche est adressée par slug et non par identifiant : c'est ce que porte l'URL du front, et
// le slug est unique en base.
public sealed class GetTemtemBySlugUseCase(
    UseCaseAuthorizer authorizer,
    ITemtemCreatureRepository creatureRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Temtem;

    public async Task<TemtemDetailView> Execute(string slug)
    {
        return await creatureRepository.FindBySlug(slug)
            ?? throw AppException.NotFound("TEMTEM_NOT_FOUND", $"Aucun Temtem pour le slug « {slug} ».");
    }
}
