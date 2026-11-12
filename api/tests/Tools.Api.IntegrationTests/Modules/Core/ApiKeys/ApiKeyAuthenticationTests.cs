using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Security.Infrastructure;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.ApiKeys;

// Une clé est jugée sur son scope, jamais sur un rôle : elle n'ouvre que les use cases qui en
// déclarent un, et seulement si elle le porte. Chaque passage est tracé dans le journal.
public sealed class ApiKeyAuthenticationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public ApiKeyAuthenticationTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.ApiKeys.Reset();
        factory.AppLogs.Reset();
        Mails.Clear();
        factory.Services.GetRequiredService<InMemoryNotificationRepository>().Clear();
    }

    private RecordingMailSender Mails => factory.Services.GetRequiredService<RecordingMailSender>();

    private static object MailPayload => new
    {
        to = new[] { "user@example.com" },
        subject = "Rapport",
        text = "Corps du rapport."
    };

    private static object NotificationPayload => new
    {
        title = "Sync terminée",
        body = "L'import est terminé.",
        type = "SUCCESS",
        targetMinRole = "ADMIN"
    };

    private HttpClient ClientWithKey(string key)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, key);
        return client;
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private List<(string Action, JsonElement Metadata)> ApiKeyLogs() =>
        factory.AppLogs.Entries
            .Where(entry => entry.AreaCode == "API_KEY")
            .Select(entry => (entry.ActionCode, JsonDocument.Parse(entry.Metadata).RootElement.Clone()))
            .ToList();

    [Fact]
    public async Task A_key_with_the_scope_runs_the_use_case()
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.CoreMailSend]);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Rapport", Mails.LastCommand?.Subject);
    }

    [Fact]
    public async Task The_former_internal_url_accepts_a_key_too()
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.CoreNotificationsPublish]);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/internal/notifications", NotificationPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_key_without_the_scope_is_forbidden()
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.TemtemSync]);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_SCOPE", await ErrorCode(response));
        Assert.Null(Mails.LastCommand);
    }

    // Refus par défaut : un use case qui ne déclare aucun scope est fermé à toutes les clés,
    // même celles qui portent tous les scopes du catalogue.
    [Theory]
    [InlineData("/admin/api-keys")]
    [InlineData("/notifications")]
    [InlineData("/users")]
    public async Task A_key_never_opens_a_use_case_without_scope(string url)
    {
        var key = factory.ApiKeys.Add(Enum.GetValues<ApiKeyScope>());

        using var response = await ClientWithKey(key).GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("API_KEY_NOT_ALLOWED", await ErrorCode(response));
    }

    [Theory]
    [InlineData(true, false, "REVOKED")]
    [InlineData(false, true, "EXPIRED")]
    public async Task A_revoked_or_expired_key_is_unauthorized(bool revoked, bool expired, string reason)
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.CoreMailSend], revoked: revoked, expired: expired);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_API_KEY", await ErrorCode(response));
        Assert.Null(Mails.LastCommand);

        var log = Assert.Single(ApiKeyLogs());
        Assert.Equal("REJECTED", log.Action);
        Assert.Equal(reason, log.Metadata.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task An_unknown_key_is_unauthorized_and_never_logged_in_full()
    {
        const string unknown = "tk_ceci-n-est-pas-une-vraie-cle-0123456789abcdef";

        using var response = await ClientWithKey(unknown).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var log = Assert.Single(ApiKeyLogs());
        Assert.Equal("UNKNOWN", log.Metadata.GetProperty("reason").GetString());
        Assert.Equal(unknown[..12], log.Metadata.GetProperty("keyPrefix").GetString());
        Assert.DoesNotContain(unknown, factory.AppLogs.Entries.Single().Metadata);
    }

    // Un en-tête de clé désigne un service : une clé refusée ne retombe pas sur le jeton.
    [Fact]
    public async Task An_invalid_key_is_not_rescued_by_a_valid_user_token()
    {
        var client = factory.CreateClientWithRole("TECH");
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "tk_inconnue-0123456789");

        using var response = await client.PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(Mails.LastCommand);
    }

    [Fact]
    public async Task A_successful_use_is_not_logged_but_updates_the_last_use()
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.CoreMailSend]);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(ApiKeyLogs());
        Assert.Equal(1, Assert.Single(factory.ApiKeys.Touches).Id);
    }

    // Une clé valide employée hors de ses scopes est tracée avec le refus, pas comme un succès.
    [Fact]
    public async Task A_use_refused_by_the_use_case_is_logged_with_its_status()
    {
        var key = factory.ApiKeys.Add([ApiKeyScope.TemtemSync]);

        using var response = await ClientWithKey(key).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var log = Assert.Single(ApiKeyLogs());
        Assert.Equal("USED", log.Action);
        Assert.Equal(1, log.Metadata.GetProperty("apiKeyId").GetInt64());
        Assert.Equal("/mail", log.Metadata.GetProperty("path").GetString());
        Assert.Null(factory.AppLogs.Entries.Single().UserId);
        Assert.Equal(403, log.Metadata.GetProperty("status").GetInt32());
    }

    // L'ancien secret n'a pas de ligne en base : seul le journal dit qui l'emploie encore.
    [Fact]
    public async Task The_legacy_secret_still_works_and_is_logged_as_such()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationDefaults.LegacyHeaderName, ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("LEGACY_USED", Assert.Single(ApiKeyLogs()).Action);
    }

    [Fact]
    public async Task The_legacy_secret_does_not_open_a_use_case_without_scope()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationDefaults.LegacyHeaderName, ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.GetAsync("/admin/api-keys");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Les deux portes mènent au même use case : un utilisateur TECH reste jugé sur son rôle.
    [Theory]
    [InlineData("TECH", HttpStatusCode.NoContent)]
    [InlineData("USER", HttpStatusCode.Forbidden)]
    public async Task A_user_is_still_judged_on_role(string role, HttpStatusCode expected)
    {
        using var response = await factory.CreateClientWithRole(role).PostAsJsonAsync("/mail", MailPayload);

        Assert.Equal(expected, response.StatusCode);
        Assert.Empty(ApiKeyLogs());
    }
}
