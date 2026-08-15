using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Serilog;
using Tools.ApiCore.Modules.Auth.Api;
using Tools.ApiCore.Modules.Auth.Application.Ports.Google;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;
using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Application.Services;
using Tools.ApiCore.Modules.Auth.Application.Usecases.Google;
using Tools.ApiCore.Modules.Auth.Application.Usecases.Password;
using Tools.ApiCore.Modules.Auth.Application.Usecases.Registration;
using Tools.ApiCore.Modules.Auth.Application.Usecases.Session;
using Tools.ApiCore.Modules.Auth.Application;
using Tools.ApiCore.Modules.Auth.Domain;
using Tools.ApiCore.Modules.Auth.Infrastructure.Google;
using Tools.ApiCore.Modules.Auth.Infrastructure.Jwt;
using Tools.ApiCore.Modules.Auth.Infrastructure.Password;
using Tools.ApiCore.Modules.Auth.Infrastructure.Persistence;
using Tools.ApiCore.Modules.Auth.Infrastructure.Registration;
using Tools.ApiCore.Modules.Common.Api.Errors;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Common.Infrastructure;
using Tools.ApiCore.Modules.Health.Api;
using Tools.ApiCore.Modules.Health.Application;
using Tools.ApiCore.Modules.Health.Infrastructure;
using Tools.ApiCore.Modules.Mail.Api;
using Tools.ApiCore.Modules.Mail.Application.Ports;
using Tools.ApiCore.Modules.Mail.Application.Services;
using Tools.ApiCore.Modules.Mail.Application.Usecases;
using Tools.ApiCore.Modules.Mail.Application;
using Tools.ApiCore.Modules.Mail.Infrastructure;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;
using Tools.ApiCore.Modules.Security.Infrastructure;
using Tools.ApiCore.Modules.Users.Api;
using Tools.ApiCore.Modules.Users.Application.Usecases;
using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ApiProblemDetailsFactory>();

            return new BadRequestObjectResult(
                problemDetailsFactory.CreateValidation(context.HttpContext, context.ModelState));
        };
    });
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problemDetailsFactory = context.HttpContext.RequestServices
            .GetRequiredService<ApiProblemDetailsFactory>();

        problemDetailsFactory.Enrich(context.ProblemDetails, context.HttpContext);
    };
});
builder.Services.AddSingleton<ApiProblemDetailsFactory>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var connectionString = BuildPostgresConnectionString(builder.Configuration)
    ?? builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Connection string Postgres manquante");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection(GoogleOAuthOptions.SectionName));
builder.Services.Configure<SmtpMailOptions>(builder.Configuration.GetSection(SmtpMailOptions.SectionName));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection(PasswordResetOptions.SectionName));
builder.Services.Configure<RegistrationOptions>(builder.Configuration.GetSection(RegistrationOptions.SectionName));
builder.Services.AddCors(options => options.AddPolicy("ToolsFrontend", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddHttpClient<IGoogleOAuthClient, GoogleOAuthClient>(client => client.BaseAddress = new Uri("https://oauth2.googleapis.com/"));
builder.Services.AddScoped<IAuthRepository, PostgresAuthRepository>();
builder.Services.AddScoped<IGoogleAuthRepository, PostgresGoogleAuthRepository>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IGoogleIdentityVerifier, GoogleOidcTokenVerifier>();
builder.Services.AddSingleton<IGoogleOAuthStateStore, GoogleOAuthStateStore>();
builder.Services.AddSingleton<RefreshTokenCookieManager>();
builder.Services.AddSingleton<IMailSender, SmtpMailSender>();
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<SendMailUseCase>();
builder.Services.AddCoreJwtAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();
builder.Services.AddScoped<UseCaseAuthorizer>();
builder.Services.AddScoped<IPasswordResetRepository, PostgresPasswordResetRepository>();
builder.Services.AddScoped<IUserCredentialsRepository, PostgresUserCredentialsRepository>();
builder.Services.AddScoped<IUserAuthProviderRepository, PostgresUserAuthProviderRepository>();
builder.Services.AddScoped<RequestPasswordResetUseCase>();
builder.Services.AddScoped<ResetPasswordUseCase>();
builder.Services.AddScoped<SetUserPasswordUseCase>();
builder.Services.AddScoped<IRegistrationRepository, PostgresRegistrationRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, PostgresEmailVerificationRepository>();
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<VerifyEmailUseCase>();

// Le nettoyage planifié n'a pas lieu d'être dans les tests d'intégration.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<PasswordResetCleanupService>();
    builder.Services.AddHostedService<EmailVerificationCleanupService>();
}
builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddScoped<GoogleIdentityAuthenticationService>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RefreshSessionUseCase>();
builder.Services.AddScoped<CreateElectronSessionUseCase>();
builder.Services.AddScoped<GetGoogleAuthorizationUrlUseCase>();
builder.Services.AddScoped<CompleteGoogleOAuthLoginUseCase>();
builder.Services.AddScoped<PostgresSession>();
builder.Services.AddScoped<ITransactionManager, PostgresTransactionManager>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<GetMyProfileUseCase>();
builder.Services.AddScoped<IHealthRepository, PostgresHealthRepository>();
builder.Services.AddScoped<CheckReadinessUseCase>();

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("ToolsFrontend");

// L'ordre n'est pas négociable : l'authentification identifie l'appelant, l'autorisation
// décide ensuite si la route lui est ouverte.
app.UseAuthentication();

// La FallbackPolicy s'applique aussi aux requêtes qui n'ont atteint aucun endpoint : sans
// cette garde, une URL inconnue répondrait 401 au lieu de 404. Le front traite les 401 comme
// une session expirée et tenterait un refresh sur une simple faute de frappe.
app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseAuthorization();

var applicationVersion = builder.Configuration["Application:Version"]
    ?? "unknown";
var gitSha = builder.Configuration["Application:GitSha"]
    ?? "unknown";

// Interrogée par le healthcheck du conteneur et par Watchtower, qui ne présentent aucun
// jeton : elle doit rester anonyme malgré l'authentification exigée par défaut.
app.MapGet("/version", () => new
{
    service = "api-core",
    runtime = ".NET",
    version = applicationVersion,
    gitSha,
    environment = app.Environment.EnvironmentName
}).AllowAnonymous();

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    MapErrorContractTestingEndpoints(app);

    // Route témoin de l'authentification par défaut : elle ne déclare aucune sécurité et
    // n'appelle aucun use case sécurisé. Elle doit malgré tout être refusée sans jeton,
    // sans quoi une route ajoutée par distraction serait ouverte au monde entier.
    app.MapGet("/_tests/unsecured", () => Results.Ok(new { reached = true }));
}

app.Run();

static string? BuildPostgresConnectionString(IConfiguration configuration)
{
    var host = configuration["DB_HOST"];
    var portValue = configuration["DB_PORT"];
    var database = configuration["DB_NAME"];
    var username = configuration["DB_USERNAME"];
    var password = configuration["DB_PASSWORD"];

    var databaseVariables = new[] { host, portValue, database, username, password };

    if (databaseVariables.All(string.IsNullOrWhiteSpace))
    {
        return null;
    }

    if (databaseVariables.Any(string.IsNullOrWhiteSpace))
    {
        throw new InvalidOperationException(
            "Les variables DB_HOST, DB_PORT, DB_NAME, DB_USERNAME et DB_PASSWORD doivent toutes être renseignées.");
    }

    if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
    {
        throw new InvalidOperationException("La variable DB_PORT doit contenir un port PostgreSQL valide.");
    }

    return new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = database,
        Username = username,
        Password = password
    }.ConnectionString;
}

static void MapErrorContractTestingEndpoints(WebApplication app)
{
    app.MapGet("/_tests/errors/{kind}", (string kind) =>
    {
        throw kind switch
        {
            "validation" => AppException.Validation(
                "TEST_VALIDATION_ERROR",
                "Erreur de validation de test."),
            "not-found" => AppException.NotFound(
                "TEST_NOT_FOUND_ERROR",
                "Ressource de test introuvable."),
            "conflict" => AppException.Conflict(
                "TEST_CONFLICT_ERROR",
                "Conflit de test."),
            "forbidden" => AppException.Forbidden(
                "TEST_FORBIDDEN_ERROR",
                "Accès refusé pour le test."),
            "unavailable" => AppException.Unavailable(
                "TEST_UNAVAILABLE_ERROR",
                "Dépendance indisponible pour le test."),
            "internal" => throw new InvalidOperationException("Erreur technique de test."),
            _ => throw AppException.Validation(
                "TEST_UNKNOWN_ERROR_KIND",
                "Type d'erreur de test inconnu.")
        };
    }).AllowAnonymous(); // Ces endpoints vérifient le contrat d'erreur, pas l'authentification.
}

public partial class Program
{
}
