using System.Diagnostics;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Services;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Translation.Application.Commands;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Application.Services;
using Tools.Api.Modules.Translation.Application.Views;
using Tools.Api.Modules.Translation.Domain;

namespace Tools.Api.Modules.Translation.Application.Usecases;

// US-1 / US-4 / US-5 : un texte (page Traduction) ou les fragments d'une page (extension).
// Chaque appel laisse une ligne TRANSLATION dans le journal : langues, volume, durée — jamais le texte.
public sealed class TranslateUseCase(
    UseCaseAuthorizer authorizer,
    TranslationService translationService,
    AppLogService appLogService) : SecuredUseCase(authorizer)
{
    public const string LogArea = "TRANSLATION";

    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Translation;
    protected override ApiKeyScope? RequiredScope => ApiKeyScope.TranslationTranslate;

    public async Task<TranslationView> Execute(TranslateCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = TranslationRequest.Create(command.Texts, command.Source, command.Target);

        ProviderTranslation result;
        try
        {
            result = await translationService.Translate(request, cancellationToken);
        }
        catch (AppException error)
        {
            await Log("FAILED", request, null, stopwatch.ElapsedMilliseconds, error.Code);
            throw;
        }

        var durationMs = stopwatch.ElapsedMilliseconds;
        await Log("TRANSLATED", request, result.Detected, durationMs, null);

        return new TranslationView(
            result.Texts,
            request.Source ?? result.Detected?.Language,
            request.Target,
            result.Detected,
            request.Characters,
            durationMs,
            translationService.ProviderName);
    }

    // Un utilisateur est identifié par user_id, une clé par son id en métadonnée.
    private Task Log(string action, TranslationRequest request, DetectedLanguage? detected, long durationMs, string? error)
    {
        var apiKey = (Caller as ApiKeyCaller)?.ApiKey;

        return appLogService.Log(new AppLogCommand(
            ModuleId: null,
            AreaCode: LogArea,
            ActionCode: action,
            UserId: (Caller as UserCaller)?.User.UserId,
            Metadata: new
            {
                Source = request.Source,
                DetectedSource = detected?.Language,
                Target = request.Target,
                Characters = request.Characters,
                Segments = request.Texts.Count,
                DurationMs = durationMs,
                Provider = translationService.ProviderName,
                ApiKeyId = apiKey?.Id,
                ApiKeyName = apiKey?.Name,
                Error = error
            }));
    }
}
