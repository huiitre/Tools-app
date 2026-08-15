using Microsoft.AspNetCore.Hosting;
using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tools.ApiCore.IntegrationTests.Fakes;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Services;
using Tools.ApiCore.Modules.Auth.Domain;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Mail.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.IntegrationTests.Fixtures;

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

            // Les flux de mot de passe sont testés sans PostgreSQL.
            services.AddSingleton<InMemoryAuthStore>();
            services.RemoveAll<IAuthRepository>();
            services.AddScoped<IAuthRepository, InMemoryAuthRepository>();
            services.RemoveAll<IUserAuthProviderRepository>();
            services.AddScoped<IUserAuthProviderRepository, InMemoryUserAuthProviderRepository>();
            services.RemoveAll<IUserCredentialsRepository>();
            services.AddScoped<IUserCredentialsRepository, InMemoryUserCredentialsRepository>();
            services.RemoveAll<IPasswordResetRepository>();
            services.AddScoped<IPasswordResetRepository, InMemoryPasswordResetRepository>();
            services.RemoveAll<IRegistrationRepository>();
            services.AddScoped<IRegistrationRepository, InMemoryRegistrationRepository>();
            services.RemoveAll<IEmailVerificationRepository>();
            services.AddScoped<IEmailVerificationRepository, InMemoryEmailVerificationRepository>();
            services.RemoveAll<ITransactionManager>();
            services.AddScoped<ITransactionManager, NoOpTransactionManager>();
        });
    }

    public InMemoryAuthStore Store => Services.GetRequiredService<InMemoryAuthStore>();

    // Le token est produit par le vrai ITokenService : émission et lecture des rôles
    // sont donc testées ensemble, exactement comme en production.
    public HttpClient CreateClientWithRoles(params string[] roles) => CreateClientForUser(1, roles);

    public HttpClient CreateClientForUser(long userId, params string[] roles)
    {
        var token = Services.GetRequiredService<ITokenService>().CreateAccessToken(
            new AuthUser(userId, "admin@example.com", true, "HUMAN"),
            roles,
            new Dictionary<string, string>());

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

