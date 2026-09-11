using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Sync;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.GameServers;

public sealed class ModIconResolverTests
{
    [Theory]
    [InlineData("https://modrinth.com/mod/jtmvUHXj", true)]
    [InlineData("https://www.modrinth.com/datapack/some-slug", true)]
    [InlineData("https://modrinth.com/user/someone", false)]
    [InlineData("https://www.curseforge.com/projects/227639", false)]
    [InlineData("pas une url", false)]
    public void Modrinth_recognises_only_its_project_pages(string modUrl, bool expected) =>
        Assert.Equal(expected, Modrinth(new StubHandler(_ => Json("[]"))).Supports(modUrl));

    [Theory]
    [InlineData("https://www.curseforge.com/projects/227639", true)]
    [InlineData("https://curseforge.com/projects/1644071", true)]
    [InlineData("https://www.curseforge.com/minecraft/mc-mods/the-twilight-forest", false)]
    [InlineData("https://www.curseforge.com/projects/abc", false)]
    [InlineData("https://modrinth.com/mod/jtmvUHXj", false)]
    public void CurseForge_recognises_only_numeric_project_urls(string modUrl, bool expected) =>
        Assert.Equal(expected, CurseForge(new StubHandler(_ => Json("{}"))).Supports(modUrl));

    [Fact]
    public async Task Modrinth_resolves_every_url_in_one_call_whether_it_names_the_id_or_the_slug()
    {
        var handler = new StubHandler(_ => Json(
            """
            [
              { "id": "jtmvUHXj", "slug": "accessories", "icon_url": "https://cdn.modrinth.com/accessories.png" },
              { "id": "lhGA9TYQ", "slug": "architectury-api", "icon_url": null }
            ]
            """));

        var icons = await Modrinth(handler).ResolveAsync(
        [
            "https://modrinth.com/mod/jtmvUHXj",
            "https://modrinth.com/mod/accessories",
            "https://modrinth.com/mod/lhGA9TYQ"
        ]);

        Assert.Equal(2, icons.Count);
        Assert.Equal("https://cdn.modrinth.com/accessories.png", icons["https://modrinth.com/mod/jtmvUHXj"]);
        Assert.Equal("https://cdn.modrinth.com/accessories.png", icons["https://modrinth.com/mod/accessories"]);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v2/projects", request.Uri.AbsolutePath);
    }

    [Fact]
    public async Task CurseForge_sends_every_id_in_one_call_and_keeps_the_thumbnails()
    {
        var handler = new StubHandler(_ => Json(
            """
            { "data": [
              { "id": 227639, "logo": { "thumbnailUrl": "https://media.forgecdn.net/twilight.png" } },
              { "id": 1644071, "logo": null }
            ] }
            """));

        var icons = await CurseForge(handler).ResolveAsync(
        [
            "https://www.curseforge.com/projects/227639",
            "https://www.curseforge.com/projects/1644071"
        ]);

        Assert.Equal("https://media.forgecdn.net/twilight.png", Assert.Single(icons).Value);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("""{"modIds":[227639,1644071]}""", request.Body);
    }

    public static TheoryData<string> Outages => ["http-500", "invalid-json", "network", "timeout"];

    [Theory]
    [MemberData(nameof(Outages))]
    public async Task An_outage_is_never_an_error_it_only_leaves_icons_empty(string outage)
    {
        Func<HttpRequestMessage, HttpResponseMessage> respond = outage switch
        {
            "http-500" => _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            "invalid-json" => _ => Json("<html>maintenance</html>"),
            "network" => _ => throw new HttpRequestException("connexion refusée"),
            _ => _ => throw new TaskCanceledException("délai dépassé")
        };

        IModIconResolver[] resolvers = [Modrinth(new StubHandler(respond)), CurseForge(new StubHandler(respond))];
        foreach (var resolver in resolvers)
        {
            var icons = await resolver.ResolveAsync(
                ["https://modrinth.com/mod/jtmvUHXj", "https://www.curseforge.com/projects/227639"]);
            Assert.Empty(icons);
        }
    }

    private static ModrinthIconResolver Modrinth(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.modrinth.com/") },
        NullLogger<ModrinthIconResolver>.Instance);

    private static CurseForgeIconResolver CurseForge(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.curseforge.com/") },
        NullLogger<CurseForgeIconResolver>.Instance);

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri Uri, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!, body));
            return respond(request);
        }
    }
}
