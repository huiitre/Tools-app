using Npgsql;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();

var connectionString = BuildPostgresConnectionString(builder.Configuration)
    ?? builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Connection string Postgres manquante");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<PostgresSession>();
builder.Services.AddScoped<ITransactionManager, PostgresTransactionManager>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<ListUsersUseCase>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();

var applicationVersion = builder.Configuration["Application:Version"]
    ?? "unknown";
var gitSha = builder.Configuration["Application:GitSha"]
    ?? "unknown";

app.MapGet("/health", () => new { status = "ok" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapGet("/version", () => new
{
    service = "api-core",
    runtime = ".NET",
    version = applicationVersion,
    gitSha,
    environment = app.Environment.EnvironmentName
});

app.MapControllers();

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

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString().ToLowerInvariant()
    });
}
