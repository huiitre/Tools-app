using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Connection string Postgres manquante");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<PostgresSession>();
builder.Services.AddScoped<ITransactionManager, PostgresTransactionManager>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<ListUsersUseCase>();

var app = builder.Build();

var applicationVersion = builder.Configuration["Application:Version"]
    ?? "unknown";
var gitSha = builder.Configuration["Application:GitSha"]
    ?? "unknown";

app.MapGet("/health", () => new { status = "ok" });

app.MapGet("/version", () => new
{
    version = applicationVersion,
    gitSha,
    environment = app.Environment.EnvironmentName
});

app.MapControllers();

app.Run();
