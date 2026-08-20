using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;

namespace Tools.Api.IntegrationTests.Modules.Core.Auth;

public sealed class PasswordTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;
    private readonly InMemoryAuthStore store;

    public PasswordTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        store = factory.Store;

        // La factory est partagée par la classe : chaque test repart d'un état vierge.
        store.Reset();
        factory.Services.GetRequiredService<RecordingMailSender>().Clear();
    }

    private RecordingMailSender Mails => factory.Services.GetRequiredService<RecordingMailSender>();

    // ---------- Demande de réinitialisation ----------

    [Fact]
    public async Task Reset_request_sends_a_link_to_an_account_having_a_password()
    {
        store.AddUser(1, "user@example.com", withPasswordProvider: true);
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset-request", new { email = "user@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Mails.LastCommand;
        Assert.NotNull(sent);
        Assert.Equal("user@example.com", Assert.Single(sent.To));

        // Le lien pointe vers le front et porte le jeton enregistré.
        var token = Assert.Single(store.ResetTokens).Key;
        Assert.Contains($"/auth/reset-password?token={token}", sent.Text);
    }

    [Fact]
    public async Task Reset_request_sends_nothing_to_a_google_only_account()
    {
        // Un compte sans provider PASSWORD n'a pas de mot de passe à réinitialiser.
        store.AddUser(2, "google@example.com", withPasswordProvider: false);
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset-request", new { email = "google@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(store.ResetTokens);
        Assert.Null(Mails.LastCommand);
    }

    [Fact]
    public async Task Reset_request_answers_the_same_for_an_unknown_email()
    {
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset-request", new { email = "inconnu@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RESET_REQUESTED", body.GetProperty("status").GetString());
        Assert.Null(Mails.LastCommand);
    }

    [Fact]
    public async Task Reset_request_replaces_the_previous_token()
    {
        store.AddUser(1, "user@example.com", withPasswordProvider: true);
        var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync("/auth/password/reset-request", new { email = "user@example.com" });
        using var second = await client.PostAsJsonAsync("/auth/password/reset-request", new { email = "user@example.com" });

        // Une seule demande active par utilisateur.
        Assert.Single(store.ResetTokens);
    }

    // ---------- Validation du lien ----------

    [Fact]
    public async Task Reset_sets_the_new_password_and_consumes_the_token()
    {
        store.AddUser(1, "user@example.com", withPasswordProvider: true);
        store.ResetTokens["jeton-valide"] = (1, DateTime.UtcNow.AddMinutes(30));
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset", new { token = "jeton-valide", password = "nouveau-mot-de-passe" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(store.ResetTokens);
        Assert.True(BCrypt.Net.BCrypt.Verify("nouveau-mot-de-passe", store.PasswordHashes[1]));
    }

    [Fact]
    public async Task Reset_is_refused_for_an_unknown_token()
    {
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset", new { token = "jeton-inconnu", password = "peu-importe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_PASSWORD_RESET_TOKEN", await ReadCode(response));
    }

    [Fact]
    public async Task Reset_is_refused_for_an_expired_token()
    {
        store.AddUser(1, "user@example.com", withPasswordProvider: true);
        store.ResetTokens["jeton-expire"] = (1, DateTime.UtcNow.AddMinutes(-1));
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/password/reset", new { token = "jeton-expire", password = "nouveau-mot-de-passe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_PASSWORD_RESET_TOKEN", await ReadCode(response));
    }

    // ---------- Définir son mot de passe depuis les options ----------

    [Fact]
    public async Task Set_password_creates_the_password_provider_for_a_google_account()
    {
        store.AddUser(5, "google@example.com", withPasswordProvider: false);
        var client = factory.CreateClientForUser(5, "READ_ONLY");

        using var response = await client.PatchAsJsonAsync("/auth/password", new { password = "mon-mot-de-passe" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // Le compte peut désormais se connecter par mot de passe et demander une réinitialisation.
        Assert.Contains((5L, "PASSWORD"), store.Providers);
        Assert.True(BCrypt.Net.BCrypt.Verify("mon-mot-de-passe", store.PasswordHashes[5]));
    }

    [Fact]
    public async Task Set_password_updates_an_existing_password_without_duplicating_the_provider()
    {
        store.AddUser(6, "user@example.com", withPasswordProvider: true);
        var client = factory.CreateClientForUser(6, "READ_ONLY");

        using var response = await client.PatchAsJsonAsync("/auth/password", new { password = "nouveau" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(store.Providers, entry => entry == (6L, "PASSWORD"));
        Assert.True(BCrypt.Net.BCrypt.Verify("nouveau", store.PasswordHashes[6]));
    }

    // L'absence de jeton et l'absence de rôle sont des règles transverses : elles sont
    // couvertes par AuthenticationTests et AuthorizationTests.

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
