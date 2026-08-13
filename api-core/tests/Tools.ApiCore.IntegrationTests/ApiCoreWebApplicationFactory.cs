using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class ApiCoreWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=127.0.0.1;Port=5432;Database=tests;Username=tests;Password=tests"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMailSender>();
            services.AddSingleton<RecordingMailSender>();
            services.AddSingleton<IMailSender>(provider => provider.GetRequiredService<RecordingMailSender>());
        });
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
