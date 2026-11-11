using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Security.Infrastructure;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Translation;

// Membres du module (READ_ONLY) ou clé translation:translate ; chaque traduction est journalisée
// sans son texte.
public sealed class TranslationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public TranslationTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Engine.Reset();
        factory.AppLogs.Reset();
        factory.ApiKeys.Reset();
    }

    private FakeTranslationProvider Engine => factory.Services.GetRequiredService<FakeTranslationProvider>();

    private HttpClient Member(string role = "READ_ONLY") =>
        factory.CreateClientForUser(42, new Dictionary<string, string> { ["translation"] = role });

    private HttpClient WithKey(params ApiKeyScope[] scopes)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, factory.ApiKeys.Add(scopes));
        return client;
    }

    private static object Payload(string[] texts, string? source = "fr", string target = "en") =>
        new { texts, source, target };

    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private List<JsonElement> TranslationLogs() =>
        factory.AppLogs.Entries.Where(e => e.AreaCode == "TRANSLATION")
            .Select(e => JsonDocument.Parse(e.Metadata).RootElement.Clone()).ToList();

    [Fact]
    public async Task A_member_translates_texts_in_order()
    {
        using var response = await Member().PostAsJsonAsync("/translation/translate", Payload(["Bonjour", "Au revoir"]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Body(response);
        Assert.Equal(["[en] Bonjour", "[en] Au revoir"], body.GetProperty("texts").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal(16, body.GetProperty("characters").GetInt32());
        Assert.Equal("fake", body.GetProperty("provider").GetString());
    }

    [Fact]
    public async Task Each_translation_is_logged_without_its_text()
    {
        using var response = await Member().PostAsJsonAsync("/translation/translate", Payload(["Texte confidentiel"]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = Assert.Single(factory.AppLogs.Entries, e => e.AreaCode == "TRANSLATION");
        Assert.Equal("TRANSLATED", entry.ActionCode);
        Assert.Equal(42, entry.UserId);
        Assert.DoesNotContain("confidentiel", entry.Metadata);

        var log = TranslationLogs().Single();
        Assert.Equal("fr", log.GetProperty("source").GetString());
        Assert.Equal("en", log.GetProperty("target").GetString());
        Assert.Equal(18, log.GetProperty("characters").GetInt32());
        Assert.True(log.GetProperty("durationMs").GetInt64() >= 0);
    }

    [Fact]
    public async Task Auto_detection_returns_the_detected_language()
    {
        using var response = await Member().PostAsJsonAsync("/translation/translate", Payload(["Bonjour"], source: null));

        var body = await Body(response);
        Assert.Equal("fr", body.GetProperty("source").GetString());
        Assert.Equal("fr", body.GetProperty("detected").GetProperty("language").GetString());
        Assert.Equal("fr", TranslationLogs().Single().GetProperty("detectedSource").GetString());
    }

    [Fact]
    public async Task A_user_outside_the_module_is_refused()
    {
        using var response = await factory.CreateClientWithRole("ADMIN")
            .PostAsJsonAsync("/translation/translate", Payload(["Bonjour"]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(Engine.LastTexts);
    }

    [Fact]
    public async Task A_key_with_the_scope_translates_and_is_named_in_the_log()
    {
        using var response = await WithKey(ApiKeyScope.TranslationTranslate)
            .PostAsJsonAsync("/translation/translate", Payload(["Bonjour"]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = Assert.Single(factory.AppLogs.Entries, e => e.AreaCode == "TRANSLATION");
        Assert.Null(entry.UserId);
        Assert.Equal(1, TranslationLogs().Single().GetProperty("apiKeyId").GetInt64());
    }

    [Fact]
    public async Task A_key_without_the_scope_is_refused()
    {
        using var response = await WithKey(ApiKeyScope.CoreMailSend)
            .PostAsJsonAsync("/translation/translate", Payload(["Bonjour"]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task More_than_5000_characters_is_refused_before_reaching_the_engine()
    {
        using var response = await Member()
            .PostAsJsonAsync("/translation/translate", Payload([new string('a', 3000), new string('b', 2001)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("TRANSLATION_TEXT_TOO_LONG", (await Body(response)).GetProperty("code").GetString());
        Assert.Null(Engine.LastTexts);
        Assert.Empty(TranslationLogs());
    }

    [Fact]
    public async Task An_unavailable_engine_gives_503_and_a_failed_log()
    {
        Engine.Unavailable = true;

        using var response = await Member().PostAsJsonAsync("/translation/translate", Payload(["Bonjour"]));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("FAILED", Assert.Single(factory.AppLogs.Entries, e => e.AreaCode == "TRANSLATION").ActionCode);
        Assert.Equal("TRANSLATION_UNAVAILABLE", TranslationLogs().Single().GetProperty("error").GetString());
    }

    [Fact]
    public async Task Languages_and_detection_are_open_to_members_and_keys()
    {
        using var languages = await Member().GetAsync("/translation/languages");
        using var detect = await WithKey(ApiKeyScope.TranslationTranslate)
            .PostAsJsonAsync("/translation/detect", new { text = "Bonjour" });

        Assert.Equal(HttpStatusCode.OK, languages.StatusCode);
        Assert.Equal(2, (await Body(languages)).GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, detect.StatusCode);
        Assert.Equal("fr", (await Body(detect))[0].GetProperty("language").GetString());
    }
}
