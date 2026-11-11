using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Translation.Infrastructure.LibreTranslate;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Translation;

// Réponses reprises telles quelles du conteneur LibreTranslate du NAS (relevées le 11/11/2026).
public sealed class LibreTranslateProviderTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request, LastBody);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static (LibreTranslateProvider Provider, StubHandler Handler) Provider(
        Func<HttpRequestMessage, string?, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://libretranslate:5000/") };
        return (new LibreTranslateProvider(client, Options.Create(new LibreTranslateOptions()),
            NullLogger<LibreTranslateProvider>.Instance), handler);
    }

    [Fact]
    public async Task Texts_are_sent_as_an_array_and_come_back_in_order()
    {
        var (provider, handler) = Provider((_, _) =>
            Json(HttpStatusCode.OK, """{"translatedText":["Hello","Goodbye"]}"""));

        var result = await provider.Translate(["Bonjour", "Au revoir"], "fr", "en", default);

        Assert.Equal(["Hello", "Goodbye"], result.Texts);
        Assert.Null(result.Detected);
        Assert.Contains("\"q\":[\"Bonjour\",\"Au revoir\"]", handler.LastBody);
        Assert.Contains("\"source\":\"fr\"", handler.LastBody);
        Assert.DoesNotContain("api_key", handler.LastBody);
    }

    [Fact]
    public async Task Auto_detection_reads_the_language_detected_for_the_batch()
    {
        var (provider, handler) = Provider((_, _) => Json(HttpStatusCode.OK,
            """{"detectedLanguage":[{"confidence":90.0,"language":"de"},{"confidence":90.0,"language":"de"}],"translatedText":["Good day","Hello World"]}"""));

        var result = await provider.Translate(["Guten Tag", "Hallo Welt"], null, "en", default);

        Assert.Equal("de", result.Detected?.Language);
        Assert.Equal(90.0, result.Detected?.Confidence);
        Assert.Contains("\"source\":\"auto\"", handler.LastBody);
    }

    [Theory]
    [InlineData("""{"error":"zz is not supported"}""", "TRANSLATION_LANGUAGE_UNSUPPORTED")]
    [InlineData("""{"error":"Invalid request: request (5001) exceeds text limit (5000)"}""", "TRANSLATION_REJECTED")]
    public async Task A_400_is_a_refused_request(string body, string code)
    {
        var (provider, _) = Provider((_, _) => Json(HttpStatusCode.BadRequest, body));

        var error = await Assert.ThrowsAsync<AppException>(() => provider.Translate(["x"], "fr", "zz", default));

        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task A_server_error_or_an_unreachable_engine_means_unavailable()
    {
        var (failing, _) = Provider((_, _) => Json(HttpStatusCode.InternalServerError, "{}"));
        var (unreachable, _) = Provider((_, _) => throw new HttpRequestException("connexion refusée"));
        var (slow, _) = Provider((_, _) => throw new TaskCanceledException("délai dépassé"));

        foreach (var provider in new[] { failing, unreachable, slow })
        {
            var error = await Assert.ThrowsAsync<AppException>(() => provider.Translate(["x"], "fr", "en", default));
            Assert.Equal("TRANSLATION_UNAVAILABLE", error.Code);
        }
    }

    [Fact]
    public async Task Languages_and_detection_are_read_from_the_engine()
    {
        var (provider, _) = Provider((request, _) => request.RequestUri!.AbsolutePath switch
        {
            "/languages" => Json(HttpStatusCode.OK,
                """[{"code":"en","name":"English","targets":["de","fr"]},{"code":"fr","name":"French","targets":["en"]}]"""),
            _ => Json(HttpStatusCode.OK, """[{"confidence":40.0,"language":"nl"},{"confidence":90.0,"language":"de"}]""")
        });

        var languages = await provider.GetLanguages(default);
        var detected = await provider.Detect("Guten Tag", default);

        Assert.Equal(["en", "fr"], languages.Select(l => l.Code));
        Assert.Equal(["de", "fr"], languages[0].Targets);
        Assert.Equal("de", detected[0].Language);
    }
}
