using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;
using Tools.Api.Modules.Core.Auth.Application.Ports.Password;
using Tools.Api.Modules.Core.Auth.Application.Ports.Registration;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Application.Usecases.Google;
using Tools.Api.Modules.Core.Auth.Application.Usecases.Password;
using Tools.Api.Modules.Core.Auth.Application.Usecases.Registration;
using Tools.Api.Modules.Core.Auth.Application.Usecases.Session;
using Tools.Api.Modules.Core.Auth.Infrastructure.Google;
using Tools.Api.Modules.Core.Auth.Infrastructure.Jwt;
using Tools.Api.Modules.Core.Auth.Infrastructure.Password;
using Tools.Api.Modules.Core.Auth.Infrastructure.Persistence;
using Tools.Api.Modules.Core.Auth.Infrastructure.Registration;

namespace Tools.Api.Modules.Core.Auth;

// Composition du module Auth : tout ce qui sert à s'identifier.
//
// Le découpage interne reprend celui des dossiers Usecases/ et Ports/ — par méthode
// d'identification, pas par nature technique (voir docs/ARCHITECTURE.md, « Découper les use
// cases et les ports »). Ce qui est lu par plusieurs flux reste à la racine de la méthode.
public static class AuthModule
{
    public static IHostApplicationBuilder AddAuthModule(this IHostApplicationBuilder builder)
    {
        // Lu par le login, le renouvellement, la session Electron et les flux de mot de passe :
        // ce port n'appartient à aucun flux en particulier.
        builder.Services.AddScoped<IAuthRepository, PostgresAuthRepository>();

        // Signale les arrivées de comptes aux administrateurs : appelé par l'inscription, la
        // confirmation d'adresse et le premier passage Google — donc par trois flux.
        builder.Services.AddScoped<AdminSignupNotifier>();

        AddSession(builder);
        AddPasswordFlows(builder);
        AddGoogleFlows(builder);
        AddRegistrationFlows(builder);

        return builder;
    }

    // Durée de vie d'une session, quelle que soit la méthode d'identification initiale.
    private static void AddSession(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

        builder.Services.AddSingleton<ITokenService, JwtTokenService>();
        builder.Services.AddSingleton<RefreshTokenCookieManager>();
        builder.Services.AddCoreJwtAuthentication();

        builder.Services.AddScoped<AuthSessionService>();
        builder.Services.AddScoped<RefreshSessionUseCase>();
        builder.Services.AddScoped<CreateElectronSessionUseCase>();
    }

    private static void AddPasswordFlows(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<PasswordResetOptions>(
            builder.Configuration.GetSection(PasswordResetOptions.SectionName));

        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<IPasswordResetRepository, PostgresPasswordResetRepository>();
        builder.Services.AddScoped<IUserCredentialsRepository, PostgresUserCredentialsRepository>();
        builder.Services.AddScoped<IUserAuthProviderRepository, PostgresUserAuthProviderRepository>();

        builder.Services.AddScoped<LoginUseCase>();
        builder.Services.AddScoped<RequestPasswordResetUseCase>();
        builder.Services.AddScoped<ResetPasswordUseCase>();
        builder.Services.AddScoped<SetUserPasswordUseCase>();

        AddCleanup<PasswordResetCleanupService>(builder);
    }

    private static void AddGoogleFlows(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<GoogleOAuthOptions>(
            builder.Configuration.GetSection(GoogleOAuthOptions.SectionName));

        builder.Services.AddHttpClient<IGoogleOAuthClient, GoogleOAuthClient>(
            client => client.BaseAddress = new Uri("https://oauth2.googleapis.com/"));
        builder.Services.AddSingleton<IGoogleIdentityVerifier, GoogleOidcTokenVerifier>();
        builder.Services.AddSingleton<IGoogleOAuthStateStore, GoogleOAuthStateStore>();
        builder.Services.AddScoped<IGoogleAuthRepository, PostgresGoogleAuthRepository>();

        builder.Services.AddScoped<GoogleIdentityAuthenticationService>();
        builder.Services.AddScoped<GetGoogleAuthorizationUrlUseCase>();
        builder.Services.AddScoped<CompleteGoogleOAuthLoginUseCase>();
    }

    private static void AddRegistrationFlows(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<RegistrationOptions>(
            builder.Configuration.GetSection(RegistrationOptions.SectionName));

        builder.Services.AddScoped<IRegistrationRepository, PostgresRegistrationRepository>();
        builder.Services.AddScoped<IEmailVerificationRepository, PostgresEmailVerificationRepository>();

        builder.Services.AddScoped<RegisterUserUseCase>();
        builder.Services.AddScoped<VerifyEmailUseCase>();

        AddCleanup<EmailVerificationCleanupService>(builder);
    }

    // Le nettoyage planifié n'a pas lieu d'être dans les tests d'intégration : il ouvrirait une
    // connexion PostgreSQL absente et ferait échouer le démarrage de l'application en mémoire.
    private static void AddCleanup<TCleanupService>(IHostApplicationBuilder builder)
        where TCleanupService : class, IHostedService
    {
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<TCleanupService>();
        }
    }

    // Route témoin de l'authentification par défaut : elle ne déclare aucune sécurité et
    // n'appelle aucun use case sécurisé. Elle doit malgré tout être refusée sans jeton, sans
    // quoi une route ajoutée par distraction serait ouverte au monde entier.
    public static WebApplication MapUnsecuredTestingEndpoint(this WebApplication app)
    {
        app.MapGet("/_tests/unsecured", () => Results.Ok(new { reached = true }));

        return app;
    }
}
