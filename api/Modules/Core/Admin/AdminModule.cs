using Tools.Api.Modules.Core.Admin.Application.Ports;
using Tools.Api.Modules.Core.Admin.Application.Usecases;
using Tools.Api.Modules.Core.Admin.Infrastructure;

namespace Tools.Api.Modules.Core.Admin;

// Composition du module Admin : le tableau de bord d'administration.
//
// Il lit à travers les tables d'autres modules sans passer par leurs ports — c'est assumé :
// un agrégat de comptage n'a pas à traverser une couche applicative conçue pour des cas
// d'usage unitaires. En revanche il ne modifie rien, ce qui borne la dépendance à la lecture.
public static class AdminModule
{
    public static IHostApplicationBuilder AddAdminModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAdminStatsRepository, PostgresAdminStatsRepository>();
        builder.Services.AddScoped<GetAdminStatsUseCase>();

        return builder;
    }
}
