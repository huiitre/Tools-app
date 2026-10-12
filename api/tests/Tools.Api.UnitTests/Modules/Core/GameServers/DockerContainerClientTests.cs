using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;
using Xunit;

namespace Tools.Api.UnitTests.Modules.Core.GameServers;

public sealed class DockerContainerClientTests
{
    [Fact]
    public async Task Restart_posts_to_the_container_with_the_stop_timeout()
    {
        var handler = new StubHandler(HttpStatusCode.NoContent);

        await Client(handler).RestartAsync("minecraft-all-the-mons");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/containers/minecraft-all-the-mons/restart?t=180", request.Uri.PathAndQuery);
    }

    [Fact]
    public async Task Restart_surfaces_an_immediate_refusal() =>
        await Assert.ThrowsAsync<AppException>(() => Client(new StubHandler(HttpStatusCode.NotFound)).RestartAsync("inconnu"));

    private static DockerContainerClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://docker/") }, NullLogger<DockerContainerClient>.Instance);

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri Uri)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!));
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
