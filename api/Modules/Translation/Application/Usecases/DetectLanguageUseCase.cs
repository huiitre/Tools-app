using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Application.Services;

namespace Tools.Api.Modules.Translation.Application.Usecases;

// US-3 : « quelle langue est-ce ? », la plus probable en premier.
public sealed class DetectLanguageUseCase(
    UseCaseAuthorizer authorizer,
    TranslationService translationService) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Translation;
    protected override ApiKeyScope? RequiredScope => ApiKeyScope.TranslationTranslate;

    public Task<IReadOnlyList<DetectedLanguage>> Execute(string? text, CancellationToken cancellationToken) =>
        translationService.Detect(text, cancellationToken);
}
