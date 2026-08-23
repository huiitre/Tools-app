using System.Net.Http.Headers;
using Tools.Api.Modules.Core.Vpn.Application.Ports;
using Tools.Api.Modules.Core.Vpn.Application.Usecases;
using Tools.Api.Modules.Core.Vpn.Infrastructure;

namespace Tools.Api.Modules.Core.Vpn;

public static class VpnModule
{
    public static IHostApplicationBuilder AddVpnModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ListVpnPeersUseCase>();

        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("QA"))
        {
            builder.Services.AddScoped<IVpnGateway, InMemoryVpnGateway>();
        }
        else
        {
            builder.Services.AddHttpClient<IVpnGateway, WireGuardVpnGateway>(client =>
            {
                var host = builder.Configuration["WG_API_HOST"];
                var token = builder.Configuration["WG_API_TOKEN"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException(
                        "Les variables WG_API_HOST et WG_API_TOKEN doivent être renseignées.");
                }

                client.BaseAddress = new Uri($"{host.TrimEnd('/')}/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });
        }

        return builder;
    }
}
