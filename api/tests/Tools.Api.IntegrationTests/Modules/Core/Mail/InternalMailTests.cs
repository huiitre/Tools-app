using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Mail;

// Ancienne URL des services, gardée le temps de leur migration vers les clés d'API : même use case
// que POST /mail (TECH ou clé core.mail:send). Même structure que InternalNotificationsTests.
public sealed class InternalMailTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public InternalMailTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Services.GetRequiredService<RecordingMailSender>().Clear();
    }

    private static object ValidPayload => new
    {
        to = new[] { "user@example.com" },
        subject = "Rapport Dofus",
        text = "Le rapport est en pièce jointe."
    };

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, token);
        return client;
    }

    [Fact]
    public async Task Sending_with_the_shared_secret_passes_the_message_to_the_mail_service()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/mail", new
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

    [Fact]
    public async Task Sending_without_any_credential_is_unauthorized()
    {
        using var response = await factory.CreateClient()
            .PostAsJsonAsync("/internal/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.Services.GetRequiredService<RecordingMailSender>().LastCommand);
    }

    [Fact]
    public async Task Sending_with_a_wrong_secret_is_unauthorized()
    {
        var client = ClientWithToken("mauvais-secret");

        using var response = await client.PostAsJsonAsync("/internal/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.Services.GetRequiredService<RecordingMailSender>().LastCommand);
    }

    // Un préfixe correct ne doit pas davantage passer : la comparaison porte sur la valeur
    // entière, pas sur un début commun.
    [Fact]
    public async Task Sending_with_a_prefix_of_the_secret_is_unauthorized()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken[..10]);

        using var response = await client.PostAsJsonAsync("/internal/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Même use case que POST /mail : un utilisateur TECH y a droit.
    [Fact]
    public async Task A_technical_token_opens_the_route()
    {
        var client = factory.CreateClientWithRole("TECH");

        using var response = await client.PostAsJsonAsync("/internal/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_user_token_is_forbidden()
    {
        var client = factory.CreateClientWithRole("USER");

        using var response = await client.PostAsJsonAsync("/internal/mail", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(factory.Services.GetRequiredService<RecordingMailSender>().LastCommand);
    }

    [Fact]
    public async Task Sending_with_an_invalid_recipient_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/mail", new
        {
            to = new[] { "pas-un-email" },
            subject = "Sujet",
            text = "Corps"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sending_without_a_body_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/mail", new
        {
            to = new[] { "user@example.com" },
            subject = "Sujet"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
