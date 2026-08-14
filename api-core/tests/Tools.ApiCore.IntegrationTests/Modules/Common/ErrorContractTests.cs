using System.Net;
using System.Text.Json;
using Xunit;
using Tools.ApiCore.IntegrationTests.Fixtures;

namespace Tools.ApiCore.IntegrationTests.Modules.Common;

public sealed class ErrorContractTests(ApiCoreWebApplicationFactory factory)
    : IClassFixture<ApiCoreWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("validation", HttpStatusCode.BadRequest, "TEST_VALIDATION_ERROR")]
    [InlineData("not-found", HttpStatusCode.NotFound, "TEST_NOT_FOUND_ERROR")]
    [InlineData("conflict", HttpStatusCode.Conflict, "TEST_CONFLICT_ERROR")]
    [InlineData("internal", HttpStatusCode.InternalServerError, "INTERNAL_ERROR")]
    public async Task Error_endpoint_returns_the_expected_problem_contract(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/_tests/errors/{kind}");
        request.Headers.Add("X-Request-Id", "error-contract-test");

        using var response = await client.SendAsync(request);
        var problem = await ReadJson(response);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("type", out _));
        Assert.True(problem.TryGetProperty("message", out var message));
        Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
        Assert.Equal("/_tests/errors/" + kind, problem.GetProperty("instance").GetString());
        Assert.Equal("error-contract-test", problem.GetProperty("requestId").GetString());
        Assert.Equal("error-contract-test", response.Headers.GetValues("X-Request-Id").Single());
    }

    [Fact]
    public async Task Unknown_route_uses_the_shared_problem_contract_and_request_id()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/route-that-does-not-exist");
        request.Headers.Add("X-Request-Id", "route-not-found-test");

        using var response = await client.SendAsync(request);
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("ROUTE_NOT_FOUND", problem.GetProperty("code").GetString());
        Assert.Equal("La route demandée est introuvable.", problem.GetProperty("message").GetString());
        Assert.Equal("route-not-found-test", problem.GetProperty("requestId").GetString());
        Assert.Equal("route-not-found-test", response.Headers.GetValues("X-Request-Id").Single());
    }

    [Fact]
    public async Task Invalid_request_id_is_replaced_by_a_generated_value()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/route-that-does-not-exist");
        request.Headers.Add("X-Request-Id", "invalid request id");

        using var response = await client.SendAsync(request);
        var problem = await ReadJson(response);
        var responseRequestId = response.Headers.GetValues("X-Request-Id").Single();

        Assert.Matches("^[a-f0-9]{32}$", responseRequestId);
        Assert.Equal(responseRequestId, problem.GetProperty("requestId").GetString());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
