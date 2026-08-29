using System.Net;
using System.Net.Http.Json;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Riot;

// Éprouve ce que la migration du module Riot pouvait casser sans que la compilation le voie :
// le routage, la liaison des paramètres, et surtout les rôles — l'API Java les portait sur ses
// routes, l'API C# uniquement dans ses use cases.
public sealed class RiotAuthorizationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private const string RiotModuleCode = "RIOT";

    private HttpClient ClientInRiot(string role, long userId = InMemoryValorantAuthRepository.OwnerUserId) =>
        factory.CreateClientForUser(userId, new Dictionary<string, string> { [RiotModuleCode] = role });

    [Fact]
    public async Task Catalogue_refuse_un_appel_anonyme()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/riot/valorant/bundles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalogue_refuse_un_utilisateur_hors_du_module()
    {
        // Rôle global élevé, mais aucun accès au module : le module prime.
        using var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.GetAsync("/riot/valorant/bundles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalogue_repond_a_un_lecteur_du_module()
    {
        using var client = ClientInRiot("READ_ONLY");

        var bundles = await client.GetFromJsonAsync<List<ValorantBundleView>>("/riot/valorant/bundles");

        Assert.NotNull(bundles);
        Assert.Equal(InMemoryValorantCatalogRepository.ExistingBundleId, Assert.Single(bundles).Id);
    }

    [Fact]
    public async Task Un_pack_inconnu_rend_404()
    {
        using var client = ClientInRiot("READ_ONLY");

        using var response = await client.GetAsync("/riot/valorant/bundles/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Un_pack_se_retrouve_par_son_asset_id()
    {
        using var client = ClientInRiot("READ_ONLY");

        var bundle = await client.GetFromJsonAsync<ValorantBundleView>(
            $"/riot/valorant/bundles/by-asset/{InMemoryValorantCatalogRepository.ExistingBundleAssetId}");

        Assert.Equal(InMemoryValorantCatalogRepository.ExistingBundleId, bundle!.Id);
    }

    [Fact]
    public async Task La_liste_des_comptes_ne_rend_que_ceux_de_l_appelant()
    {
        using var owner = ClientInRiot("READ_ONLY");
        using var other = ClientInRiot("READ_ONLY", userId: 42);

        var mine = await owner.GetFromJsonAsync<List<object>>("/riot/valorant/accounts");
        var theirs = await other.GetFromJsonAsync<List<object>>("/riot/valorant/accounts");

        Assert.Single(mine!);
        Assert.Empty(theirs!);
    }

    [Fact]
    public async Task Un_compte_qui_n_est_pas_le_sien_est_introuvable()
    {
        using var client = ClientInRiot("READ_ONLY");

        using var response = await client.GetAsync(
            $"/riot/valorant/my-skins?accountId={InMemoryValorantAuthRepository.ForeignAccountId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task La_boutique_exige_un_compte_ou_un_jeton()
    {
        using var client = ClientInRiot("USER");

        using var response = await client.GetAsync("/riot/valorant/store");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task La_boutique_est_refusee_a_un_simple_lecteur()
    {
        // Le Java exigeait USER sur la route et READ_ONLY dans le use case : c'est le plus strict
        // des deux qui devait survivre à la migration.
        using var client = ClientInRiot("READ_ONLY");

        using var response = await client.GetAsync(
            $"/riot/valorant/store?accountId={InMemoryValorantAuthRepository.OwnedAccountId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ajouter_un_skin_possede_est_refuse_a_un_simple_lecteur()
    {
        using var client = ClientInRiot("READ_ONLY");

        using var response = await client.PostAsJsonAsync(
            "/riot/valorant/my-skins",
            new { skinId = 1, accountId = InMemoryValorantAuthRepository.OwnedAccountId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Cette route n'avait plus aucune protection après la migration : le Java la gardait par un
    // @RequiredRole(ADMIN) sur la route, et le notifieur qu'elle appelle n'exige rien.
    [Theory]
    [InlineData("READ_ONLY", HttpStatusCode.Forbidden)]
    [InlineData("USER", HttpStatusCode.Forbidden)]
    [InlineData("TECH", HttpStatusCode.Forbidden)]
    [InlineData("ADMIN", HttpStatusCode.NoContent)]
    public async Task La_passe_de_suivi_est_reservee_aux_administrateurs(string role, HttpStatusCode expected)
    {
        using var client = ClientInRiot(role);

        using var response = await client.PostAsync("/riot/valorant/watchlist/admin/sync", null);

        Assert.Equal(expected, response.StatusCode);
    }

    // Seule l'autorisation est vérifiée ici : ce qui suit dépend de PostgreSQL et du CDN des
    // assets, hors de portée d'un test d'intégration HTTP.
    [Theory]
    [InlineData("USER", true)]
    [InlineData("TECH", false)]
    public async Task La_synchronisation_du_catalogue_est_reservee_a_TECH(string role, bool refuse)
    {
        using var client = ClientInRiot(role);

        using var response = await client.PostAsync("/riot/valorant/sync", null);

        Assert.Equal(refuse, response.StatusCode == HttpStatusCode.Forbidden);
    }
}
