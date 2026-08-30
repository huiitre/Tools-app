using System.Net;
using System.Net.Http.Json;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;
using Tools.Api.Modules.Temtem.Types.Application.Views;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Temtem;

// Le catalogue est en lecture seule, mais il n'est pas public : le module Temtem doit être ouvert
// à l'appelant. Ces tests éprouvent le routage, la liaison des paramètres et les rôles — le SQL,
// lui, est vérifié contre la base réelle.
public sealed class TemtemCatalogueTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private const string TemtemModuleCode = "TEMTEM";

    private HttpClient ClientInTemtem(string role) =>
        factory.CreateClientForUser(1, new Dictionary<string, string> { [TemtemModuleCode] = role });

    [Theory]
    [InlineData("/temtem/types")]
    [InlineData("/temtem/creatures")]
    [InlineData("/temtem/creatures/mimit")]
    public async Task Le_catalogue_refuse_un_appel_anonyme(string route)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/temtem/types")]
    [InlineData("/temtem/creatures")]
    [InlineData("/temtem/creatures/mimit")]
    public async Task Le_catalogue_refuse_un_utilisateur_hors_du_module(string route)
    {
        // Rôle global le plus élevé, mais aucun accès au module : le module prime.
        using var client = factory.CreateClientWithRole("OWNER");

        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Les_types_repondent_a_un_lecteur_du_module()
    {
        using var client = ClientInTemtem("READ_ONLY");

        var types = await client.GetFromJsonAsync<List<TemtemTypeView>>("/temtem/types");

        Assert.NotNull(types);
        Assert.Contains(types, type => type.Slug == "eau");
    }

    [Fact]
    public async Task Le_catalogue_rend_les_temtem_en_resume()
    {
        using var client = ClientInTemtem("READ_ONLY");

        var creatures = await client.GetFromJsonAsync<List<TemtemSummaryView>>("/temtem/creatures");

        Assert.NotNull(creatures);
        Assert.Contains(creatures, creature => creature.Slug == InMemoryTemtemCatalogueRepository.MonoTypeSlug);
    }

    [Fact]
    public async Task Un_temtem_a_type_unique_n_a_pas_de_second_type()
    {
        using var client = ClientInTemtem("READ_ONLY");

        var detail = await client.GetFromJsonAsync<TemtemDetailView>(
            $"/temtem/creatures/{InMemoryTemtemCatalogueRepository.MonoTypeSlug}");

        Assert.NotNull(detail);
        Assert.Null(detail.Temtem.Type2);
    }

    [Fact]
    public async Task La_fiche_porte_le_resume_les_techniques_et_les_traits()
    {
        using var client = ClientInTemtem("READ_ONLY");

        var detail = await client.GetFromJsonAsync<TemtemDetailView>(
            $"/temtem/creatures/{InMemoryTemtemCatalogueRepository.DoubleTypeSlug}");

        Assert.NotNull(detail);
        Assert.Equal(InMemoryTemtemCatalogueRepository.DoubleTypeSlug, detail.Temtem.Slug);
        Assert.NotNull(detail.Temtem.Type2);

        // Le résumé est imbriqué, pas recopié : la fiche et la carte du catalogue portent les
        // mêmes champs, définis une seule fois.
        var learned = Assert.Single(detail.Techniques);
        Assert.Equal("LEVEL", learned.Source);
        Assert.Equal(1, learned.Level);
        Assert.Contains("SINGLE_OPPONENT", learned.Technique.Targets);
        Assert.Single(detail.Traits);
    }

    [Fact]
    public async Task Un_slug_inconnu_rend_404()
    {
        using var client = ClientInTemtem("READ_ONLY");

        using var response = await client.GetAsync("/temtem/creatures/nexistepas");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
