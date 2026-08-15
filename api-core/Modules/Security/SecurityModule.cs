using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Infrastructure;

namespace Tools.ApiCore.Modules.Security;

// Composition du module Security : l'autorisation par rôle, portée par les use cases, et le
// catalogue des rôles attribuables.
//
// Le pipeline HTTP ne rend aucun 403 (voir docs/SECURITY.md) ; tout ce qui décide d'un droit
// est enregistré ici. HttpContextAccessor accompagne HttpCurrentUserProvider, seul consommateur
// du contexte ambiant : le déclarer dans ce module évite qu'un déplacement dans la racine de
// composition ne laisse ce provider sans sa dépendance.
public static class SecurityModule
{
    public static IHostApplicationBuilder AddSecurityModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();
        builder.Services.AddScoped<UseCaseAuthorizer>();

        builder.Services.AddScoped<IRoleRepository, PostgresRoleRepository>();
        builder.Services.AddScoped<ListRolesUseCase>();

        return builder;
    }
}
