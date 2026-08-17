using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;
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

        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddScoped<ModuleAuthorizationProbe>();
        }

        return builder;
    }

    // Sonde d'autorisation par module, mappée uniquement en environnement Testing comme les
    // endpoints du contrat d'erreur. Aucun use case du Core n'appartient à un module — les
    // premiers viendront avec le métier migré depuis Java. Sans cette sonde, la règle « le rôle
    // exigé se lit dans le module » ne serait vérifiable qu'en dehors du pipeline réel, donc
    // jamais sur le chemin qui décide vraiment d'un accès.
    public static WebApplication MapModuleAuthorizationTestingEndpoint(this WebApplication app)
    {
        app.MapGet("/_tests/module-authorization", async (ModuleAuthorizationProbe probe) =>
            Results.Ok(await probe.Execute()));

        return app;
    }
}

// Exige USER dans le module Todolist. Les deux exigences sont volontairement distinctes : un
// appelant peut échouer parce que le module ne lui est pas ouvert, ou parce que son rôle à
// l'intérieur du module ne suffit pas, et les deux refus doivent rester discernables.
public sealed class ModuleAuthorizationProbe(UseCaseAuthorizer authorizer)
    : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;

    protected override ModuleCode? RequiredModule => ModuleCode.Todolist;

    public Task<ModuleAuthorizationProbeResult> Execute()
    {
        return Task.FromResult(new ModuleAuthorizationProbeResult(
            CurrentUser.UserId,
            CurrentUser.HighestRoleIn(ModuleCode.Todolist)?.ToCode()));
    }
}

public sealed record ModuleAuthorizationProbeResult(long UserId, string? ModuleRole);
