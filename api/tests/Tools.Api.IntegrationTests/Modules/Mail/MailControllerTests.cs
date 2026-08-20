using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.IntegrationTests.Fakes;

namespace Tools.Api.IntegrationTests.Modules.Mail;

public sealed class MailControllerTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
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
        var client = factory.CreateClientWithRole("TECH");

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
        var client = factory.CreateClientWithRole(role);

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    public async Task Send_is_refused_below_the_technical_level(string role)
    {
        var client = factory.CreateClientWithRole(role);

        using var response = await client.PostAsJsonAsync("/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    // Les rôles absents, inconnus ou cumulés relèvent de UseCaseAuthorizer et non du mail :
    // ils sont couverts par AuthorizationTests.

    // L'absence de jeton et les jetons invalides ne sont pas propres au mail :
    // ils sont couverts par AuthenticationTests.

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
