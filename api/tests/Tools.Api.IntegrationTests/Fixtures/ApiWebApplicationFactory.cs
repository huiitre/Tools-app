using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tools.Api.Modules.Core.Auth.Infrastructure.Jwt;
using Tools.Api.Modules.Core.Auth.Application.Ports.Registration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Domain;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Mail.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Ports.Password;
using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Admin.Application.Ports;
using Tools.Api.Modules.Core.Notifications.Application.Ports;
using Tools.Api.Modules.Core.Realtime.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Users.Application;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fixtures;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
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

            services.RemoveAll<IGameServerRepository>();
            services.AddSingleton<InMemoryGameServerRepository>();
            services.AddSingleton<IGameServerRepository>(provider => provider.GetRequiredService<InMemoryGameServerRepository>());
            services.RemoveAll<IGameServerPollingRepository>();
            services.AddSingleton<IGameServerPollingRepository>(provider => provider.GetRequiredService<InMemoryGameServerRepository>());
            services.RemoveAll<IGameServerDashboardRepository>();
            services.AddSingleton<IGameServerDashboardRepository>(provider => provider.GetRequiredService<InMemoryGameServerRepository>());
            services.RemoveAll<IGameServerStatusProvider>();
            services.AddSingleton<FakeGameServerStatusProvider>();
            services.AddSingleton<IGameServerStatusProvider>(provider => provider.GetRequiredService<FakeGameServerStatusProvider>());
            services.RemoveAll<ISteamAppDetailsProvider>();
            services.AddSingleton<FakeSteamAppDetailsProvider>();
            services.AddSingleton<ISteamAppDetailsProvider>(provider => provider.GetRequiredService<FakeSteamAppDetailsProvider>());
            services.RemoveAll<IGameServersManifestProvider>();
            services.AddSingleton<FakeGameServersManifestProvider>();
            services.AddSingleton<IGameServersManifestProvider>(provider => provider.GetRequiredService<FakeGameServersManifestProvider>());

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

            services.RemoveAll<IRealtimePublisher>();
            services.AddSingleton<RecordingRealtimePublisher>();
            services.AddSingleton<IRealtimePublisher>(
                provider => provider.GetRequiredService<RecordingRealtimePublisher>());
            services.RemoveAll<IRecipientResolver>();
            services.AddSingleton<InMemoryRecipientResolver>();
            services.AddSingleton<IRecipientResolver>(
                provider => provider.GetRequiredService<InMemoryRecipientResolver>());
        });
    }

    public InMemoryAuthStore Store => Services.GetRequiredService<InMemoryAuthStore>();

    // Le token est produit par le vrai ITokenService : émission et lecture du rôle sont donc
    // testées ensemble, exactement comme en production.
    public HttpClient CreateClientWithRole(string? role) => CreateClientForUser(1, role);

    public HttpClient CreateClientForUser(long userId, string? role = null) =>
        CreateClientForUser(userId, new Dictionary<string, string>(), role);

    // Variante avec des rôles de module, sous la forme portée par le claim : un module associé
    // au rôle que l'utilisateur y détient.
    public HttpClient CreateClientForUser(
        long userId,
        IReadOnlyDictionary<string, string> moduleRoles,
        string? role = null)
    {
        var token = Services.GetRequiredService<ITokenService>().CreateAccessToken(
            new AuthUser(userId, "admin@example.com", true, "HUMAN"),
            role,
            moduleRoles);

        return ClientWithToken(token);
    }

    // Client porteur d'un jeton forgé de toutes pièces. Sert à éprouver les formes de claims
    // que l'API accepte encore en lecture sans plus jamais les émettre : un jeton émis avant
    // le passage au rôle unique reste valide le temps de sa durée de vie, et ne peut donc pas
    // être reproduit par ITokenService.
    public HttpClient CreateClientWithForgedClaims(long userId, params Claim[] claims)
    {
        var issuer = Services.GetRequiredService<IOptions<JwtOptions>>().Value.Issuer;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));

        Claim[] allClaims =
        [
            new("tokenType", "ACCESS"),
            new("isActive", "true", ClaimValueTypes.Boolean),
            new("userType", "HUMAN"),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            .. claims
        ];

        var token = new JwtSecurityToken(
            issuer,
            null,
            allClaims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return ClientWithToken(new JwtSecurityTokenHandler().WriteToken(token));
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
