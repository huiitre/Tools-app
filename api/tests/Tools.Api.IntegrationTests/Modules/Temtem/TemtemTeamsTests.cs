using System.Net;
using System.Net.Http.Json;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Temtem.Teams.Application.Views;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Temtem;

// Les équipes sont la seule partie du module qui écrit. Ces tests éprouvent ce qu'aucune
// contrainte SQL ne garantit — six places, quatre techniques, technique réellement apprise — et
// le cloisonnement entre utilisateurs.
//
// La fixture n'est pas partagée : chaque classe de test part d'un jeu d'équipes vierge.
public sealed class TemtemTeamsTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string TemtemModuleCode = "TEMTEM";
    private const long OwnerId = 1;
    private const long StrangerId = 2;

    private readonly ApiWebApplicationFactory factory;

    public TemtemTeamsTests(ApiWebApplicationFactory factory) => this.factory = factory;

    private HttpClient ClientFor(long userId, string role = "USER") =>
        factory.CreateClientForUser(userId, new Dictionary<string, string> { [TemtemModuleCode] = role });

    private static async Task<TemtemTeamView> CreateTeam(HttpClient client, string name, int? temtemId = null)
    {
        using var response = await client.PostAsJsonAsync("/temtem/teams", new { Name = name, TemtemId = temtemId });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TemtemTeamView>())!;
    }

    [Fact]
    public async Task Les_equipes_refusent_un_appel_anonyme()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/temtem/teams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Un_lecteur_du_module_ne_peut_pas_composer_d_equipe()
    {
        // Le catalogue est ouvert en READ_ONLY, les équipes non : elles s'écrivent.
        using var client = ClientFor(OwnerId, "READ_ONLY");

        using var response = await client.GetAsync("/temtem/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task La_creation_depuis_le_catalogue_place_le_temtem_dans_la_foulee()
    {
        using var client = ClientFor(OwnerId);

        var team = await CreateTeam(client, "Depuis la popup", InMemoryTemtemCatalogueRepository.DoubleTypeId);

        var member = Assert.Single(team.Members);
        Assert.Equal(1, member.Slot);
        Assert.Equal(InMemoryTemtemCatalogueRepository.DoubleTypeSlug, member.Temtem.Slug);
    }

    [Fact]
    public async Task Deux_equipes_du_meme_nom_sont_refusees()
    {
        using var client = ClientFor(OwnerId);
        await CreateTeam(client, "Doublon");

        using var response = await client.PostAsJsonAsync("/temtem/teams", new { Name = "  doublon  " });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Un_nom_vide_est_refuse()
    {
        using var client = ClientFor(OwnerId);

        using var response = await client.PostAsJsonAsync("/temtem/teams", new { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Une_septieme_place_est_refusee()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Equipe pleine");

        for (var member = 0; member < 6; member++)
        {
            using var added = await client.PostAsJsonAsync(
                $"/temtem/teams/{team.Id}/members",
                new { TemtemId = InMemoryTemtemCatalogueRepository.MonoTypeId });

            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        }

        using var response = await client.PostAsJsonAsync(
            $"/temtem/teams/{team.Id}/members",
            new { TemtemId = InMemoryTemtemCatalogueRepository.MonoTypeId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Une_place_liberee_est_reprise_par_le_membre_suivant()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Trou a reboucher", InMemoryTemtemCatalogueRepository.MonoTypeId);

        using var second = await client.PostAsJsonAsync(
            $"/temtem/teams/{team.Id}/members",
            new { TemtemId = InMemoryTemtemCatalogueRepository.DoubleTypeId });
        var withTwo = (await second.Content.ReadFromJsonAsync<TemtemTeamView>())!;

        using var removed = await client.DeleteAsync($"/temtem/teams/{team.Id}/members/{withTwo.Members[0].Id}");
        removed.EnsureSuccessStatusCode();

        using var third = await client.PostAsJsonAsync(
            $"/temtem/teams/{team.Id}/members",
            new { TemtemId = InMemoryTemtemCatalogueRepository.MonoTypeId });
        var refilled = (await third.Content.ReadFromJsonAsync<TemtemTeamView>())!;

        Assert.Equal([1, 2], refilled.Members.Select(member => member.Slot));
    }

    [Fact]
    public async Task Un_temtem_inconnu_ne_rejoint_aucune_equipe()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Temtem inconnu");

        using var response = await client.PostAsJsonAsync(
            $"/temtem/teams/{team.Id}/members",
            new { TemtemId = 4242 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Quatre_techniques_au_maximum()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Trop de techniques", InMemoryTemtemCatalogueRepository.DoubleTypeId);
        var memberId = team.Members[0].Id;

        using var response = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{memberId}/techniques",
            new { TechniqueIds = new[] { 1, 2, 3, 4, 5 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Une_technique_que_le_temtem_n_apprend_pas_est_refusee()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Technique impossible", InMemoryTemtemCatalogueRepository.DoubleTypeId);

        using var response = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{team.Members[0].Id}/techniques",
            new { TechniqueIds = new[] { InMemoryTemtemCatalogueRepository.UnlearnableTechniqueId } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Les_techniques_retenues_remplacent_les_precedentes()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Remplacement", InMemoryTemtemCatalogueRepository.DoubleTypeId);
        var memberId = team.Members[0].Id;

        using var first = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{memberId}/techniques",
            new
            {
                TechniqueIds = new[]
                {
                    InMemoryTemtemCatalogueRepository.LearnableTechniqueId,
                    InMemoryTemtemCatalogueRepository.OtherLearnableTechniqueId
                }
            });
        first.EnsureSuccessStatusCode();

        using var second = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{memberId}/techniques",
            new { TechniqueIds = new[] { InMemoryTemtemCatalogueRepository.LearnableTechniqueId } });
        var updated = (await second.Content.ReadFromJsonAsync<TemtemTeamView>())!;

        var technique = Assert.Single(updated.Members[0].Techniques);
        Assert.Equal(InMemoryTemtemCatalogueRepository.LearnableTechniqueId, technique.Id);
    }

    [Fact]
    public async Task Une_liste_vide_efface_les_techniques_du_membre()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Effacement", InMemoryTemtemCatalogueRepository.DoubleTypeId);
        var memberId = team.Members[0].Id;

        using var filled = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{memberId}/techniques",
            new { TechniqueIds = new[] { InMemoryTemtemCatalogueRepository.LearnableTechniqueId } });
        filled.EnsureSuccessStatusCode();

        using var cleared = await client.PutAsJsonAsync(
            $"/temtem/teams/{team.Id}/members/{memberId}/techniques",
            new { TechniqueIds = Array.Empty<int>() });
        var updated = (await cleared.Content.ReadFromJsonAsync<TemtemTeamView>())!;

        Assert.Empty(updated.Members[0].Techniques);
    }

    [Fact]
    public async Task L_equipe_d_un_autre_est_introuvable_et_non_interdite()
    {
        using var owner = ClientFor(OwnerId);
        var team = await CreateTeam(owner, "Chasse gardee");

        using var stranger = ClientFor(StrangerId);

        // 404 et non 403 : confirmer l'existence de l'équipe renseignerait déjà l'intrus.
        using var renamed = await stranger.PatchAsJsonAsync($"/temtem/teams/{team.Id}", new { Name = "Volee" });
        using var deleted = await stranger.DeleteAsync($"/temtem/teams/{team.Id}");
        using var added = await stranger.PostAsJsonAsync(
            $"/temtem/teams/{team.Id}/members",
            new { TemtemId = InMemoryTemtemCatalogueRepository.MonoTypeId });

        Assert.Equal(HttpStatusCode.NotFound, renamed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, added.StatusCode);
    }

    [Fact]
    public async Task Chacun_ne_voit_que_ses_equipes()
    {
        using var owner = ClientFor(OwnerId);
        await CreateTeam(owner, "Visible par son proprietaire");

        using var stranger = ClientFor(StrangerId);
        var teams = await stranger.GetFromJsonAsync<List<TemtemTeamView>>("/temtem/teams");

        Assert.NotNull(teams);
        Assert.DoesNotContain(teams, team => team.Name == "Visible par son proprietaire");
    }

    [Fact]
    public async Task Une_equipe_supprimee_disparait_de_la_liste()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "A supprimer");

        using var response = await client.DeleteAsync($"/temtem/teams/{team.Id}");
        var teams = await client.GetFromJsonAsync<List<TemtemTeamView>>("/temtem/teams");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(teams);
        Assert.DoesNotContain(teams, remaining => remaining.Id == team.Id);
    }

    [Fact]
    public async Task Une_equipe_se_renomme_avec_son_propre_nom_sans_conflit()
    {
        using var client = ClientFor(OwnerId);
        var team = await CreateTeam(client, "Nom stable");

        using var response = await client.PatchAsJsonAsync($"/temtem/teams/{team.Id}", new { Name = "Nom stable" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
