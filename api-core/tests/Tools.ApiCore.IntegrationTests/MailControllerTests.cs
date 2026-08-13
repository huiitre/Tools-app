using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public sealed class MailControllerTests(ApiCoreWebApplicationFactory factory)
    : IClassFixture<ApiCoreWebApplicationFactory>
{
    private static readonly object ValidPayload = new
    {
        to = new[] { "user@example.com" },
        subject = "Rapport Dofus",
        text = "Le rapport est en pièce jointe."
    };

    [Fact]
    public async Task Send_passes_the_message_to_the_mail_service()
    {
        var client = factory.CreateClientWithRoles("TECH");

        using var response = await client.PostAsJsonAsync("/mail", new
        {
            to = new[] { "user@example.com" },
            subject = "Rapport Dofus",
            text = "Le rapport est en pièce jointe.",
            attachments = new[]
            {
                new
                {
                    fileName = "report.txt",
                    contentType = "text/plain",
                    contentBase64 = Convert.ToBase64String("rapport"u8.ToArray())
                }
            }
        });

        var sent = factory.Services.GetRequiredService<RecordingMailSender>().LastCommand;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(sent);
        Assert.Equal("Rapport Dofus", sent.Subject);
        var attachment = Assert.Single(sent.Attachments!);
        Assert.Equal("report.txt", attachment.FileName);
        Assert.Equal("rapport", System.Text.Encoding.UTF8.GetString(attachment.Content));
    }

    [Theory]
    [InlineData("TECH")]
    [InlineData("ADMIN")]
    [InlineData("OWNER")]
    public async Task Send_is_allowed_from_the_technical_level(string role)
    {
        var client = factory.CreateClientWithRoles(role);

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Send_uses_the_highest_role_of_the_token()
    {
        // Un utilisateur cumulant plusieurs rôles est jugé sur le plus permissif.
        var client = factory.CreateClientWithRoles("USER", "ADMIN", "READ_ONLY");

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    public async Task Send_is_refused_below_the_technical_level(string role)
    {
        var client = factory.CreateClientWithRoles(role);

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Send_is_refused_for_a_token_without_any_role()
    {
        var client = factory.CreateClientWithRoles();

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Send_is_refused_for_an_unknown_role_code()
    {
        // Un code absent de l'énumération ne doit jamais accorder de droit.
        var client = factory.CreateClientWithRoles("SUPER_ADMIN");

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Send_is_refused_without_a_token()
    {
        var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await ReadCode(response));
    }

    [Fact]
    public async Task Send_is_refused_with_an_invalid_token()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(response));
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
