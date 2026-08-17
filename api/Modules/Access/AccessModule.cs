using Tools.Api.Modules.Access.Application.Ports;
using Tools.Api.Modules.Access.Application.Usecases;
using Tools.Api.Modules.Access.Infrastructure;

namespace Tools.Api.Modules.Access;

// Composition du module Access : les modules fonctionnels de l'application et les accès des
// utilisateurs à ces modules.
//
// Il ne s'appelle pas « Modules » pour éviter `Modules/Modules` et un namespace
// `Tools.Api.Modules.Modules` — le mot désigne ici un module *fonctionnel* (Dofus,
// Palworld…), pas un module de code. `Access` nomme la responsabilité réelle : qui a accès à
// quoi, et avec quel rôle.
public static class AccessModule
{
    public static IHostApplicationBuilder AddAccessModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IModuleRepository, PostgresModuleRepository>();
        builder.Services.AddScoped<IModuleMembershipRepository, PostgresModuleMembershipRepository>();

        builder.Services.AddScoped<ListModulesUseCase>();
        builder.Services.AddScoped<CreateModuleUseCase>();
        builder.Services.AddScoped<UpdateModuleUseCase>();
        builder.Services.AddScoped<ListModuleMembersUseCase>();
        builder.Services.AddScoped<GrantModuleAccessUseCase>();
        builder.Services.AddScoped<ChangeModuleRoleUseCase>();
        builder.Services.AddScoped<RevokeModuleAccessUseCase>();

        return builder;
    }
}
