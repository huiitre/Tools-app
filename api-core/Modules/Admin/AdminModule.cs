using Tools.ApiCore.Modules.Admin.Application.Ports;
using Tools.ApiCore.Modules.Admin.Application.Usecases;
using Tools.ApiCore.Modules.Admin.Infrastructure;

namespace Tools.ApiCore.Modules.Admin;

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
