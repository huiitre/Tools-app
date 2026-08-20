using System.Net;
using Tools.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Health;

// Les routes de diagnostic sont appelées par le healthcheck Docker et par Watchtower, qui
// ne présentent aucun jeton. Elles doivent rester joignables anonymement, et cette garantie
// ne doit rien devoir à la chance : l'authentification par défaut les fermerait toutes si
// elles n'étaient pas déclarées anonymes.
//
// Un oubli ici ne casse pas un écran : il fait redémarrer un conteneur en boucle en QA.
public sealed class DiagnosticsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/version")]
    public async Task Diagnostic_routes_answer_without_a_token(string route)
    {
        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_answers_without_a_token()
    {
        // La readiness interroge PostgreSQL, absent en environnement Testing : elle répondra
        // 503. Ce qui est vérifié ici n'est pas son verdict mais le fait qu'elle soit
        // atteinte — un 401 signifierait que le healthcheck ne peut plus rien mesurer.
        using var response = await client.GetAsync("/health/ready");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
