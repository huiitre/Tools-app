using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

public sealed class MailControllerTests(ApiCoreWebApplicationFactory factory)
    : IClassFixture<ApiCoreWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Send_passes_the_message_to_the_mail_service()
    {
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
}
