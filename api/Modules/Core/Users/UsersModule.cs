using Tools.Api.Modules.Core.Users.Application;
using Tools.Api.Modules.Core.Users.Application.Usecases;
using Tools.Api.Modules.Core.Users.Infrastructure;

namespace Tools.Api.Modules.Core.Users;

// Composition du module Users : le profil utilisateur.
//
// Les moyens d'identification (mot de passe, Google, session) appartiennent au module Auth,
// pas ici : voir docs/ARCHITECTURE.md, section « Contrat de routes HTTP ».
public static class UsersModule
{
    public static IHostApplicationBuilder AddUsersModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
        builder.Services.AddScoped<GetMyProfileUseCase>();
        builder.Services.AddScoped<ListUsersUseCase>();
        builder.Services.AddScoped<SetUserGlobalRoleUseCase>();

        return builder;
    }
}
