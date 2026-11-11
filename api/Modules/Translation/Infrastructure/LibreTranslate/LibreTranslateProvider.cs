using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Translation.Application.Ports;

namespace Tools.Api.Modules.Translation.Infrastructure.LibreTranslate;

// Adaptateur HTTP de LibreTranslate. Les textes partent toujours en tableau : la réponse a alors
// une seule forme. En détection automatique, LibreTranslate détecte une langue pour tout le lot.
public sealed class LibreTranslateProvider(
    HttpClient httpClient,
    IOptions<LibreTranslateOptions> options,
    ILogger<LibreTranslateProvider> logger) : ITranslationProvider
{
    // Sans clé configurée, `api_key` n'est pas envoyé du tout.
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "libretranslate";

    public async Task<ProviderTranslation> Translate(
        IReadOnlyList<string> texts, string? source, string target, CancellationToken cancellationToken)
    {
        using var document = await Send(HttpMethod.Post, "translate", new
        {
            q = texts,
            source = source ?? "auto",
            target,
            format = "text",
            api_key = options.Value.ApiKey
        }, cancellationToken);

        var root = document.RootElement;
        var translated = root.GetProperty("translatedText").EnumerateArray()
            .Select(item => item.GetString() ?? "")
            .ToList();

        // Forme tableau quand `q` est un tableau : une entrée par texte, toutes identiques.
        DetectedLanguage? detected = null;
        if (source is null && root.TryGetProperty("detectedLanguage", out var detection))
        {
            var first = detection.ValueKind == JsonValueKind.Array ? detection.EnumerateArray().FirstOrDefault() : detection;
            if (first.ValueKind == JsonValueKind.Object)
                detected = ReadDetection(first);
        }

        return new ProviderTranslation(translated, detected);
    }

    public async Task<IReadOnlyList<TranslationLanguage>> GetLanguages(CancellationToken cancellationToken)
    {
        using var document = await Send(HttpMethod.Get, "languages", null, cancellationToken);

        return document.RootElement.EnumerateArray()
            .Select(language => new TranslationLanguage(
                language.GetProperty("code").GetString() ?? "",
                language.GetProperty("name").GetString() ?? "",
                language.TryGetProperty("targets", out var targets)
                    ? targets.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                    : []))
            .ToList();
    }

    public async Task<IReadOnlyList<DetectedLanguage>> Detect(string text, CancellationToken cancellationToken)
    {
        using var document = await Send(HttpMethod.Post, "detect", new { q = text, api_key = options.Value.ApiKey }, cancellationToken);

        return document.RootElement.EnumerateArray()
            .Select(ReadDetection)
            .OrderByDescending(d => d.Confidence)
            .ToList();
    }

    private static DetectedLanguage ReadDetection(JsonElement element) => new(
        element.GetProperty("language").GetString() ?? "",
        element.GetProperty("confidence").GetDouble());

    // Un 400 de LibreTranslate est une demande refusée (langue non chargée, texte trop long) ;
    // tout le reste — injoignable, délai dépassé, 5xx — rend le service indisponible.
    private async Task<JsonDocument> Send(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
                request.Content = JsonContent.Create(body, options: BodyOptions);

            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException
                                      && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(error, "LibreTranslate injoignable ou trop lent sur {Path}", path);
            throw Unavailable();
        }

        using (response)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var message = ReadError(payload) ?? "Demande de traduction refusée.";
                var unsupported = message.Contains("not supported", StringComparison.OrdinalIgnoreCase);
                throw AppException.Validation(
                    unsupported ? "TRANSLATION_LANGUAGE_UNSUPPORTED" : "TRANSLATION_REJECTED",
                    unsupported ? $"Langue non prise en charge par le service de traduction : {message}" : message);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("LibreTranslate a répondu {Status} sur {Path} : {Error}",
                    (int)response.StatusCode, path, ReadError(payload));
                throw Unavailable();
            }

            try
            {
                return JsonDocument.Parse(payload);
            }
            catch (JsonException error)
            {
                logger.LogWarning(error, "Réponse illisible de LibreTranslate sur {Path}", path);
                throw Unavailable();
            }
        }
    }

    private static string? ReadError(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AppException Unavailable() =>
        AppException.Unavailable("TRANSLATION_UNAVAILABLE", "Le service de traduction est indisponible.");
}
