using Npgsql;
using Serilog;
using Microsoft.AspNetCore.Mvc;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();
builder.Services.AddScoped<UseCaseAuthorizer>();
builder.Services.AddScoped<IPasswordResetRepository, PostgresPasswordResetRepository>();
builder.Services.AddScoped<IUserCredentialsRepository, PostgresUserCredentialsRepository>();
builder.Services.AddScoped<IUserAuthProviderRepository, PostgresUserAuthProviderRepository>();
builder.Services.AddScoped<RequestPasswordResetUseCase>();
builder.Services.AddScoped<ResetPasswordUseCase>();
builder.Services.AddScoped<SetUserPasswordUseCase>();

// Le nettoyage planifié n'a pas lieu d'être dans les tests d'intégration.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<PasswordResetCleanupService>();
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
builder.Services.AddScoped<ListUsersUseCase>();
builder.Services.AddScoped<IHealthRepository, PostgresHealthRepository>();
builder.Services.AddScoped<CheckReadinessUseCase>();

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("ToolsFrontend");

var applicationVersion = builder.Configuration["Application:Version"]
    ?? "unknown";
var gitSha = builder.Configuration["Application:GitSha"]
    ?? "unknown";

app.MapGet("/version", () => new
{
    service = "api-core",
    runtime = ".NET",
    version = applicationVersion,
    gitSha,
    environment = app.Environment.EnvironmentName
});

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    MapErrorContractTestingEndpoints(app);
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
            "validation" => ApplicationException.Validation(
                "TEST_VALIDATION_ERROR",
                "Erreur de validation de test."),
            "not-found" => ApplicationException.NotFound(
                "TEST_NOT_FOUND_ERROR",
                "Ressource de test introuvable."),
            "conflict" => ApplicationException.Conflict(
                "TEST_CONFLICT_ERROR",
                "Conflit de test."),
            "forbidden" => ApplicationException.Forbidden(
                "TEST_FORBIDDEN_ERROR",
                "Accès refusé pour le test."),
            "unavailable" => ApplicationException.Unavailable(
                "TEST_UNAVAILABLE_ERROR",
                "Dépendance indisponible pour le test."),
            "internal" => throw new InvalidOperationException("Erreur technique de test."),
            _ => throw ApplicationException.Validation(
                "TEST_UNKNOWN_ERROR_KIND",
                "Type d'erreur de test inconnu.")
        };
    });
}

public partial class Program
{
}
