using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.ApiCore.IntegrationTests.Fakes;
using Tools.ApiCore.IntegrationTests.Fixtures;
using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Xunit;

namespace Tools.ApiCore.IntegrationTests.Modules.Auth;

// Inscription par mot de passe et confirmation d'adresse.
//
// Ces routes sont ouvertes : un visiteur sans session est le seul appelant possible.
public sealed class RegistrationTests : IClassFixture<ApiCoreWebApplicationFactory>
{
    private readonly ApiCoreWebApplicationFactory factory;
    private readonly InMemoryAuthStore store;

    public RegistrationTests(ApiCoreWebApplicationFactory factory)
    {
        this.factory = factory;
        store = factory.Store;

        store.Reset();
        factory.Services.GetRequiredService<RecordingMailSender>().Clear();
        factory.Services.GetRequiredService<InMemoryNotificationRepository>().Clear();
    }

    private RecordingMailSender Mails => factory.Services.GetRequiredService<RecordingMailSender>();

    private InMemoryNotificationRepository Notifications =>
        factory.Services.GetRequiredService<InMemoryNotificationRepository>();

    private static object ValidRegistration => new
    {
        name = "Yanis",
        email = "nouveau@example.com",
        password = "mon-mot-de-passe"
    };

    // ---------- Inscription ----------

    [Fact]
    public async Task Register_creates_an_inactive_account_and_sends_a_verification_link()
    {
        using var response = await factory.CreateClient().PostAsJsonAsync("/auth/register", ValidRegistration);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Le compte existe mais ne peut pas encore se connecter.
        var account = Assert.Single(store.Accounts).Value;
        Assert.False(account.IsActive);
        Assert.Null(account.EmailVerifiedAt);

        // Le lien envoyé porte le jeton enregistré.
        var token = Assert.Single(store.VerificationTokens).Key;
        var sent = Mails.LastCommand;
        Assert.NotNull(sent);
        Assert.Equal("nouveau@example.com", Assert.Single(sent.To));
        Assert.Contains($"/auth/verify-email?token={token}", sent.Text);
    }

    // ---------- Notification des administrateurs ----------

    [Fact]
    public async Task Register_notifies_the_administrators()
    {
        using var response = await factory.CreateClient().PostAsJsonAsync("/auth/register", ValidRegistration);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Nouvelle inscription", notification.Title);
        Assert.Contains("nouveau@example.com", notification.Body);
        Assert.Equal("INFO", notification.Type);

        // Ciblage par rôle minimum ADMIN : la population visée est ADMIN et au-dessus.
        Assert.Equal(["ADMIN", "OWNER"], Notifications.RoleCodesAsked);
    }

    [Fact]
    public async Task Confirming_the_address_notifies_the_administrators()
    {
        await factory.CreateClient().PostAsJsonAsync("/auth/register", ValidRegistration);
        Notifications.Clear();
        var token = Assert.Single(store.VerificationTokens).Key;

        using var response = await factory.CreateClient()
            .PostAsync($"/auth/verify-email?token={token}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Inscription confirmée", notification.Title);
        Assert.Contains("nouveau@example.com", notification.Body);
    }

    [Fact]
    public async Task Registering_again_before_confirmation_does_not_notify_twice()
    {
        await factory.CreateClient().PostAsJsonAsync("/auth/register", ValidRegistration);
        Notifications.Clear();

        // Reprise d'une inscription en attente : aucun compte de plus n'est créé.
        using var response = await factory.CreateClient().PostAsJsonAsync("/auth/register", ValidRegistration);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task Register_is_refused_when_the_address_is_already_confirmed()
    {
        // AddUser crée un compte actif dont l'adresse est confirmée.
        store.AddUser(1, "deja@example.com", withPasswordProvider: true);
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Quelqu'un",
            email = "deja@example.com",
            password = "peu-importe"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", await ReadCode(response));
        Assert.Null(Mails.LastCommand);
    }

    [Fact]
    public async Task Registering_again_before_confirmation_replaces_the_password()
    {
        var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/auth/register", ValidRegistration);
        var firstHash = Assert.Single(store.PasswordHashes).Value;

        using var second = await client.PostAsJsonAsync("/auth/register", new
        {
            name = "Yanis",
            email = "nouveau@example.com",
            password = "un-autre-mot-de-passe"
        });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // Aucun compte en double, et c'est bien le dernier mot de passe qui compte : l'API
        // Java jetait celui de la seconde tentative, laissant l'utilisateur avec un mot de
        // passe qu'il croyait avoir changé.
        Assert.Single(store.Accounts);
        var currentHash = Assert.Single(store.PasswordHashes).Value;
        Assert.NotEqual(firstHash, currentHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("un-autre-mot-de-passe", currentHash));
    }

    [Fact]
    public async Task Registering_again_replaces_the_previous_verification_token()
    {
        var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/auth/register", ValidRegistration);
        using var second = await client.PostAsJsonAsync("/auth/register", ValidRegistration);

        // Une seule demande active : le premier lien ne doit plus fonctionner.
        Assert.Single(store.VerificationTokens);
    }

    // ---------- Confirmation ----------

    [Fact]
    public async Task Verify_activates_the_account_and_consumes_the_token()
    {
        var client = factory.CreateClient();
        using var registration = await client.PostAsJsonAsync("/auth/register", ValidRegistration);
        var token = Assert.Single(store.VerificationTokens).Key;

        using var response = await client.PostAsync($"/auth/verify-email?token={token}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var account = Assert.Single(store.Accounts).Value;
        Assert.True(account.IsActive);
        Assert.NotNull(account.EmailVerifiedAt);

        // Le lien ne peut pas resservir.
        Assert.Empty(store.VerificationTokens);
    }

    [Fact]
    public async Task Verify_is_refused_for_an_unknown_token()
    {
        using var response = await factory.CreateClient()
            .PostAsync("/auth/verify-email?token=jeton-inconnu", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_EMAIL_VERIFICATION_TOKEN", await ReadCode(response));
    }

    [Fact]
    public async Task Verify_is_refused_for_an_expired_token()
    {
        store.Accounts[1] = ("Yanis", "expire@example.com", false, null);
        store.VerificationTokens["jeton-expire"] = (1, DateTime.UtcNow.AddMinutes(-1));

        using var response = await factory.CreateClient()
            .PostAsync("/auth/verify-email?token=jeton-expire", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_EMAIL_VERIFICATION_TOKEN", await ReadCode(response));
        Assert.False(store.Accounts[1].IsActive);
    }

    // ---------- Nettoyage des inscriptions abandonnées ----------

    [Fact]
    public async Task Cleanup_removes_registrations_never_confirmed()
    {
        store.Accounts[1] = ("Abandonné", "abandon@example.com", false, null);
        store.VerificationTokens["jeton-expire"] = (1, DateTime.UtcNow.AddMinutes(-1));

        var deleted = await Repository().DeleteAbandonedRegistrationsAsync(DateTime.UtcNow);

        Assert.Equal(1, deleted);
        Assert.Empty(store.Accounts);
    }

    [Fact]
    public async Task Cleanup_spares_a_registration_still_within_its_delay()
    {
        store.Accounts[1] = ("En cours", "encours@example.com", false, null);
        store.VerificationTokens["jeton-valide"] = (1, DateTime.UtcNow.AddMinutes(30));

        var deleted = await Repository().DeleteAbandonedRegistrationsAsync(DateTime.UtcNow);

        Assert.Equal(0, deleted);
        Assert.Single(store.Accounts);
    }

    [Fact]
    public async Task Cleanup_spares_a_suspended_account_whose_address_was_confirmed()
    {
        // Le cas que l'API Java supprimait : compte désactivé par un administrateur, sans
        // jeton de confirmation en cours. Son adresse ayant été confirmée un jour, il n'est
        // pas une inscription abandonnée.
        store.Accounts[1] = ("Suspendu", "suspendu@example.com", false, DateTime.UtcNow.AddYears(-1));

        var deleted = await Repository().DeleteAbandonedRegistrationsAsync(DateTime.UtcNow);

        Assert.Equal(0, deleted);
        Assert.Single(store.Accounts);
    }

    private IEmailVerificationRepository Repository() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<IEmailVerificationRepository>();

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
