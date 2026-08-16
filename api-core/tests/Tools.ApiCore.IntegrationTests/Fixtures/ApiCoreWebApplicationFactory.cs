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
using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Admin.Application.Ports;
using Tools.ApiCore.Modules.Notifications.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Users.Application;

namespace Tools.ApiCore.IntegrationTests.Fixtures;

public sealed class ApiCoreWebApplicationFactory : WebApplicationFactory<Program>
{
    // Secrets de test uniquement : aucun lien avec les environnements réels.
    public const string TestJwtSecret = "integration-tests-secret-key-0123456789";
    public const string TestInternalToken = "integration-tests-internal-token-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=127.0.0.1;Port=5432;Database=tests;Username=tests;Password=tests",
                ["JWT_SECRET"] = TestJwtSecret,
                ["INTERNAL_API_TOKEN"] = TestInternalToken
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

            // L'administration est testée sans PostgreSQL : ces doubles sont des singletons
            // pour que le test puisse relire l'état laissé par la requête.
            services.RemoveAll<IRoleRepository>();
            services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<InMemoryUserRepository>();
            services.AddSingleton<IUserRepository>(provider => provider.GetRequiredService<InMemoryUserRepository>());
            services.RemoveAll<IModuleRepository>();
            services.AddSingleton<IModuleRepository, InMemoryModuleRepository>();
            services.RemoveAll<IModuleMembershipRepository>();
            services.AddSingleton<InMemoryModuleMembershipRepository>();
            services.AddSingleton<IModuleMembershipRepository>(
                provider => provider.GetRequiredService<InMemoryModuleMembershipRepository>());
            services.RemoveAll<IAdminStatsRepository>();
            services.AddSingleton<IAdminStatsRepository, InMemoryAdminStatsRepository>();

            services.RemoveAll<INotificationRepository>();
            services.AddSingleton<InMemoryNotificationRepository>();
            services.AddSingleton<INotificationRepository>(
                provider => provider.GetRequiredService<InMemoryNotificationRepository>());
        });
    }

    public InMemoryAuthStore Store => Services.GetRequiredService<InMemoryAuthStore>();

    // Le token est produit par le vrai ITokenService : émission et lecture des rôles
    // sont donc testées ensemble, exactement comme en production.
    public HttpClient CreateClientWithRoles(params string[] roles) => CreateClientForUser(1, roles);

    public HttpClient CreateClientForUser(long userId, params string[] roles) =>
        CreateClientForUser(userId, new Dictionary<string, IReadOnlyList<string>>(), roles);

    // Variante avec des rôles de module, sous la forme portée par le claim : un module associé
    // aux rôles que l'utilisateur y détient.
    public HttpClient CreateClientForUser(
        long userId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> moduleRoles,
        params string[] roles)
    {
        var token = Services.GetRequiredService<ITokenService>().CreateAccessToken(
            new AuthUser(userId, "admin@example.com", true, "HUMAN"),
            roles,
            moduleRoles);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

