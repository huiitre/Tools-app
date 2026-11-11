using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Application.Services;

namespace Tools.Api.Modules.Translation.Application.Usecases;

// US-2 : les langues que le moteur sait traduire, pour les listes déroulantes. Elles viennent du
// moteur, jamais d'une liste en dur : le NAS ne charge qu'une partie des modèles.
public sealed class ListTranslationLanguagesUseCase(
    UseCaseAuthorizer authorizer,
    TranslationService translationService) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Translation;
    protected override ApiKeyScope? RequiredScope => ApiKeyScope.TranslationTranslate;

    public Task<IReadOnlyList<TranslationLanguage>> Execute(CancellationToken cancellationToken) =>
        translationService.GetLanguages(cancellationToken);
}
