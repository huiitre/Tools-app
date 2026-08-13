using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class ApiCoreWebApplicationFactory : WebApplicationFactory<Program>
{
    // Secret de test uniquement : aucun lien avec les environnements réels.
    public const string TestJwtSecret = "integration-tests-secret-key-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=127.0.0.1;Port=5432;Database=tests;Username=tests;Password=tests",
                ["JWT_SECRET"] = TestJwtSecret
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMailSender>();
            services.AddSingleton<RecordingMailSender>();
            services.AddSingleton<IMailSender>(provider => provider.GetRequiredService<RecordingMailSender>());
        });
    }

    // Le token est produit par le vrai ITokenService : émission et lecture des rôles
    // sont donc testées ensemble, exactement comme en production.
    public HttpClient CreateClientWithRoles(params string[] roles)
    {
        var token = Services.GetRequiredService<ITokenService>().CreateAccessToken(
            new AuthUser(1, "admin@example.com", true, "HUMAN"),
            roles,
            new Dictionary<string, string>());

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class RecordingMailSender : IMailSender
{
    public SendMailCommand? LastCommand { get; private set; }

    public Task SendAsync(SendMailCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Task.CompletedTask;
    }
}
